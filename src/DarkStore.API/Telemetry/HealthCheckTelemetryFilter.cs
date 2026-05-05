using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace DarkStore.API.Telemetry;

/// <summary>
/// Filters health-check requests out of Application Insights telemetry.
/// Without this, /health/live and /health/ready generate ~1 440 request
/// telemetry entries per day (every minute) which inflate costs and noise.
/// </summary>
public sealed class HealthCheckTelemetryFilter(ITelemetryProcessor next) : ITelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry request &&
            request.Url?.AbsolutePath.StartsWith("/health", StringComparison.OrdinalIgnoreCase) == true)
        {
            return; // drop health-check traffic
        }

        next.Process(item);
    }
}


