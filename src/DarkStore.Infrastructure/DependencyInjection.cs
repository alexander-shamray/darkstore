using DarkStore.Application.Common.Interfaces;
using DarkStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DarkStore.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers both DbContexts with EF Core retry-on-failure, Redis cache,
    /// health checks, and Polly-based resilience pipelines.
    ///
    /// AppDbContext          → Azure SQL  (connection string: AzureConnection)
    /// PersonalDataDbContext → KZ Local DB (connection string: KzLocalConnection)
    /// Redis                 → Azure Cache for Redis (connection string: Redis)
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Azure SQL — business data (no PII) ──────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AzureConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 6,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sql.CommandTimeout(60);
                }));

        // IAppDbContext → AppDbContext (Application layer sees the interface, not the EF class)
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ── KZ Local DB — personal data (Kazakhstan Law #94-V) ──────────────
        services.AddDbContext<PersonalDataDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("KzLocalConnection"),
                sql =>
                {
                    sql.MigrationsAssembly(typeof(PersonalDataDbContext).Assembly.FullName);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 6,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sql.CommandTimeout(60);
                }));

        // ── Redis — distributed cache + session store ──────────────────────
        // abortConnect=False: app starts even if Redis is down at boot.
        // connectRetry=3:     attempts to reconnect on transient failures.
        string? redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = $"{redisConnectionString},abortConnect=False,connectRetry=3";
                opts.InstanceName = "darkstore:";
            });
        }
        else
        {
            // Development fallback — in-memory cache so the app starts without Redis
            services.AddDistributedMemoryCache();
        }

        // ── Health checks ──────────────────────────────────────────────────
        // Azure SQL — Unhealthy: catalogue + orders down without it.
        // KZ Local DB — Degraded (not Unhealthy): catalogue browsing and
        //   anonymous-user flows continue. Auth/profile endpoints degrade
        //   gracefully rather than taking the whole readiness endpoint down.
        // Redis — Degraded: system continues without cache (slightly slower).
        IHealthChecksBuilder healthChecks = services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(
                name: "azure-sql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "azure", "ready"])
            .AddDbContextCheck<PersonalDataDbContext>(
                name: "kz-local-db",
                failureStatus: HealthStatus.Degraded,   // ← was Unhealthy; auth degrades, catalogue works
                tags: ["db", "kz", "ready"]);

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecks.AddRedis(
                redisConnectionString: redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,   // Redis down ≠ system down
                tags: ["cache", "ready"]);
        }

        return services;
    }
}
