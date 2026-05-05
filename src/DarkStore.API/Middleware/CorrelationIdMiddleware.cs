namespace DarkStore.API.Middleware;

/// <summary>
/// Propagates or generates a Correlation-ID for every HTTP request.
///
/// Fault-tolerance role:
///   • Ensures every request has a traceable ID across logs, downstream services, and error responses.
///   • Reads X-Correlation-ID from the incoming request — or creates a new GUID.
///   • Writes the final ID back into the response header so clients can correlate failures.
///   • Sets HttpContext.TraceIdentifier so GlobalExceptionHandler and Serilog pick it up automatically.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    private const string _correlationIdHeader = "X-Correlation-ID";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.Use(async (ctx, next) =>
        {
            string correlationId = ctx.Request.Headers[_correlationIdHeader].FirstOrDefault()
                                   ?? Guid.NewGuid().ToString("N");

            ctx.TraceIdentifier = correlationId;
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers[_correlationIdHeader] = correlationId;
                return Task.CompletedTask;
            });

            await next(ctx);
        });
}

