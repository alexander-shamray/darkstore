using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace DarkStore.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start, completion and duration of every request.
/// Slow requests (> 500 ms) are logged at Warning level.
/// Warnings surface in Application Insights as traces for perf analysis.
///
/// Pipeline order: LoggingBehavior → ValidationBehavior → AuditBehavior → Handler
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int _slowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger.LogInformation("MediatR → handling {RequestName}", requestName);

        try
        {
            TResponse response = await next();
            sw.Stop();

            if (sw.ElapsedMilliseconds > _slowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Slow request detected — {RequestName} took {ElapsedMs} ms (threshold: {ThresholdMs} ms)",
                    requestName, sw.ElapsedMilliseconds, _slowRequestThresholdMs);
            }
            else
            {
                logger.LogInformation(
                    "MediatR ✓ {RequestName} completed in {ElapsedMs} ms",
                    requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "MediatR ✗ {RequestName} failed after {ElapsedMs} ms",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

