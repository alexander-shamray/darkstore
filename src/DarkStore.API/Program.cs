using Asp.Versioning;
using Azure.Identity;
using DarkStore.API;
using DarkStore.API.Middleware;
using DarkStore.API.Services;
using DarkStore.API.Telemetry;
using DarkStore.Application;
using DarkStore.Application.Common.Interfaces;
using DarkStore.Infrastructure;
using Destructurama;
using Microsoft.ApplicationInsights.Extensibility;
using Quartz;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.RateLimiting;

// ── Serilog bootstrap logger ──────────────────────────────────────────────────
// Used only before the DI container is built so early startup errors are visible.
// Destructurama ensures [NotLogged]/[LogMasked] attributes are respected from the start.
Log.Logger = new LoggerConfiguration()
    .Destructure.UsingAttributes()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("DarkStore API starting up…");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // ── Azure Key Vault (production — move secrets out of appsettings) ──────
    // Reads: ApplicationInsights:ConnectionString, ConnectionStrings:*, Jwt:*
    // Setup: az keyvault create --name darkstore-kv --resource-group darkstore-rg
    string? keyVaultUri = builder.Configuration["AzureKeyVaultUri"];
    if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(keyVaultUri))
    {
        // Azure.Extensions.AspNetCore.Configuration.Secrets + Azure.Identity
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
    }
    // ── Serilog (full) ────────────────────────────────────────────────────
    // FIX: All sinks wrapped in WriteTo.Async() — prevents blocking the request thread.
    // FIX: Destructurama activated — [NotLogged] & [LogMasked] attributes work on commands.
    // NEW: Environment, Thread and Process enrichers for richer Application Insights queries.
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Destructure.UsingAttributes()
           .Enrich.FromLogContext()
           .Enrich.WithMachineName()
           .Enrich.WithEnvironmentName()
           .Enrich.WithThreadId()
           .Enrich.WithProcessId()
           .Enrich.WithProperty("Application", "DarkStore.API")
           .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
           .WriteTo.Async(a => a.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"))
           .WriteTo.Async(a => a.File(
               path: "logs/darkstore-.log",
               rollingInterval: RollingInterval.Day,
               retainedFileCountLimit: 30,
               fileSizeLimitBytes: 100 * 1024 * 1024,
               rollOnFileSizeLimit: true,
               outputTemplate:
                   "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"))
           // Application Insights sink — only when connection string is configured
           .WriteTo.Conditional(
               _ => !string.IsNullOrEmpty(ctx.Configuration["ApplicationInsights:ConnectionString"]),
               a => a.Async(s => s.ApplicationInsights(
                   services.GetRequiredService<TelemetryConfiguration>(),
                   TelemetryConverter.Traces))));

    // ── Core services ──────────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ── Application layer (MediatR + pipeline behaviors + FluentValidation + Mapster) ──
    builder.Services.AddApplication();

    // ── ICurrentUserService — JWT claim extraction (scoped per request) ───
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ── OpenAPI + Scalar UI (replaces Swashbuckle) ────────────────────────
    // Scalar UI available at: /scalar/v1 (development only)
    builder.Services.AddOpenApi();

    // ── API Versioning — URL segment /api/v{version}/ ─────────────────────
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true; // api-supported-versions header
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ── RFC-7807 ProblemDetails + global exception handler ───────────────
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ── Application Insights ──────────────────────────────────────────────
    string? aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(aiConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(opts =>
        {
            opts.ConnectionString = aiConnectionString;
            opts.EnableAdaptiveSampling = true;
            opts.EnableDependencyTrackingTelemetryModule = true; // SQL, HTTP, Redis auto-tracked
        });
        builder.Services.AddApplicationInsightsTelemetryProcessor<HealthCheckTelemetryFilter>();
    }

    // ── OpenTelemetry (Q3+ — deferred until OTel packages reach 1.15.3+) ──
    // CVE-2026-40894 & CVE-2026-42191 prevent using current 1.15.2 packages.
    // Uncomment when all three packages publish 1.15.3+:
    //
    // var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
    // if (!string.IsNullOrEmpty(otlpEndpoint))
    // {
    //     builder.Services.AddOpenTelemetry()
    //         .WithTracing(tracing => tracing
    //             .AddAspNetCoreInstrumentation()
    //             .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint)));
    // }

    // ── JWT Authentication ─────────────────────────────────────────────────
    // Authority and Audience are stored in Azure Key Vault in production.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.Authority = builder.Configuration["Jwt:Authority"];
            opts.Audience  = builder.Configuration["Jwt:Audience"];
            opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            opts.TokenValidationParameters.ValidateLifetime = true;
            opts.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
        });
    builder.Services.AddAuthorization();

    // ── Rate Limiting ──────────────────────────────────────────────────────
    // OTP endpoint: max 3 requests per sliding window of 1 hour.
    // Apply with [EnableRateLimiting("otp")] on the SendOtp action.
    builder.Services.AddRateLimiter(opts =>
    {
        opts.AddSlidingWindowLimiter("otp", limiter =>
        {
            limiter.PermitLimit            = 3;
            limiter.Window                 = TimeSpan.FromHours(1);
            limiter.SegmentsPerWindow      = 4;
            limiter.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit             = 0;
        });
        // General API throttle — 100 req/min per IP
        opts.AddFixedWindowLimiter("api", limiter =>
        {
            limiter.PermitLimit            = 100;
            limiter.Window                 = TimeSpan.FromMinutes(1);
            limiter.QueueProcessingOrder   = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit             = 10;
        });
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── CORS ───────────────────────────────────────────────────────────────
    // AllowCredentials() is required for SignalR WebSocket negotiation.
    // Origins read from config → Azure Key Vault in prod, appsettings in dev.
    string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(opts => opts.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

    // ── SignalR (real-time order status + courier tracking) ───────────────
    builder.Services.AddSignalR();

    // ── Response Compression (Brotli + Gzip) ─────────────────────────────
    // 60–80% smaller JSON over Kazakhstani 4G mobile connections.
    builder.Services.AddResponseCompression(opts =>
    {
        opts.EnableForHttps = true;
        opts.Providers.Add<BrotliCompressionProvider>();
        opts.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(opts =>
        opts.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(opts =>
        opts.Level = CompressionLevel.Fastest);

    // ── Output Cache (L1 in-memory, in front of Redis L2) ────────────────
    // Use [OutputCache(PolicyName = "catalog")] on GET /products
    builder.Services.AddOutputCache(opts =>
    {
        opts.AddPolicy("catalog", policy =>
            policy.Expire(TimeSpan.FromMinutes(5))
                  .SetVaryByQuery("categoryId", "page", "size")
                  .Tag("catalog"));
    });

    // ── Background Jobs (Quartz.NET) ───────────────────────────────────────
    // Persistent store: Azure SQL (same connection as AppDbContext — no extra infra).
    // Schema: run the Quartz SQL Server DDL script once per environment before first deploy.
    //   → https://github.com/quartznet/quartznet/blob/main/database/tables/tables_sqlServer.sql
    // Serializer: System.Text.Json — no Newtonsoft.Json dependency.
    builder.Services.AddQuartz(q =>
    {
        q.UsePersistentStore(store =>
        {
            // UseProperties: job data stored as plain string K/V pairs — no object serializer needed.
            // All job parameters must be strings (enforced at runtime); prevents Newtonsoft.Json dep.
            store.UseProperties = true;
            store.RetryInterval = TimeSpan.FromSeconds(15);
            store.PerformSchemaValidation = true;
            store.UseSqlServer(sql =>
                sql.ConnectionString = builder.Configuration.GetConnectionString("AzureConnection")!);
        });
        q.UseDefaultThreadPool(pool =>
            pool.MaxConcurrency = Environment.ProcessorCount * 2);

        // ── Recurring jobs ─────────────────────────────────────────────────
        // To schedule the 1С inventory sync every 5 minutes, add a job + trigger:
        //
        //   var syncKey = new JobKey("1c-sync", "inventory");
        //   q.AddJob<OneCSyncJob>(syncKey, j => j.StoreDurably());
        //   q.AddTrigger(t => t.ForJob(syncKey)
        //       .WithIdentity("1c-sync-trigger")
        //       .WithCronSchedule("0 */5 * * * ?"));   // every 5 min
    });
    builder.Services.AddQuartzHostedService(opts =>
    {
        opts.WaitForJobsToComplete = true;   // graceful shutdown — let running jobs finish
        opts.AwaitApplicationStarted = true; // don't start scheduler until app is fully ready
    });

    // ── Resilient HTTP client factory (Polly v8) ──────────────────────────
    // All typed clients (1С, Kaspi Pay, SMS, Google Maps) use:
    //   builder.Services.AddHttpClient<IFooClient, FooClient>()
    //       .AddStandardResilienceHandler(opts => { ... });

    // ── Infrastructure (DbContexts + Redis + health checks) ──────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Build the app ──────────────────────────────────────────────────────
    WebApplication app = builder.Build();

    // ── DB migrations at startup (with retry + exponential back-off) ──────
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DatabaseMigrationHelper.MigrateAsync(app.Services, app.Logger);
    }

    // ── Middleware pipeline (ORDER MATTERS!) ──────────────────────────────
    app.UseExceptionHandler();                            // GlobalExceptionHandler — must be first

    if (!app.Environment.IsDevelopment())
    {
        // HSTS: max-age 1 year; forces HTTPS for all future requests
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseResponseCompression();                         // compress before CORS headers set
    app.UseCors("FrontendPolicy");                        // CORS before auth (handles preflight)
    app.UseRateLimiter();

    // ── Security headers (CSP / clickjacking / MIME sniffing protection) ──
    app.Use(async (ctx, next) =>
    {
        ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Response.Headers.Append("X-Frame-Options", "DENY");
        ctx.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        ctx.Response.Headers.Append("Permissions-Policy",
            "geolocation=(), camera=(), microphone=(), payment=()");
        if (!app.Environment.IsDevelopment())
        {
            ctx.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob: https://storage.azure.com; " +
                "connect-src 'self' wss:; frame-ancestors 'none'");
        }
        await next();
    });

    app.UseCorrelationId();                               // X-Correlation-Id propagation
    app.UseSerilogRequestLogging(opts =>                  // structured HTTP access log
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.000}ms [{CorrelationId}]";
        opts.GetLevel = (httpCtx, _, ex) =>
            ex is not null || httpCtx.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpCtx.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
        opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        {
            diagCtx.Set("CorrelationId", httpCtx.TraceIdentifier);
            diagCtx.Set("UserAgent",     httpCtx.Request.Headers.UserAgent.ToString());
            diagCtx.Set("RemoteIP",      httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    app.UseOutputCache();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ── SignalR Hubs ──────────────────────────────────────────────────────
    // app.MapHub<OrderHub>("/hubs/order");   // Uncomment when OrderHub is implemented


    // ── OpenAPI + Scalar UI ───────────────────────────────────────────────
    // /openapi/v1.json  — raw spec (dev only; expose behind auth in prod if needed)
    // /scalar/v1        — interactive UI
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title = "Dark Store API";
            opts.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

    // ── Health-check endpoints ────────────────────────────────────────────
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate      = _ => false,   // no checks — pure liveness ping
        ResponseWriter = WriteHealthJson
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate      = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthJson
    });

    app.Run();
    return 0;
}
catch (Exception ex) when (ex is not OperationCanceledException && ex.GetType().Name != "StopTheHostException")
{
    Log.Fatal(ex, "DarkStore API terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// ── Helper: JSON health-check response ────────────────────────────────────────
static async Task WriteHealthJson(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    string result = JsonSerializer.Serialize(new
    {
        status    = report.Status.ToString(),
        timestamp = DateTimeOffset.UtcNow,
        duration  = report.TotalDuration,
        checks    = report.Entries.Select(e => new
        {
            name        = e.Key,
            status      = e.Value.Status.ToString(),
            description = e.Value.Description,
            duration    = e.Value.Duration,
            exception   = e.Value.Exception?.Message
        })
    }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    await ctx.Response.WriteAsync(result);
}
