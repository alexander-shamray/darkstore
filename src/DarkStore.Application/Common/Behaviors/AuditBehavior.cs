using DarkStore.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DarkStore.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records who triggered each command and when.
/// Runs after validation so only valid requests are audited.
/// Write-side (commands) are audited; queries are skipped to avoid noise.
///
/// Pipeline order: LoggingBehavior → ValidationBehavior → AuditBehavior → Handler
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    ILogger<AuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only audit commands (write-side). Queries carry "Query" suffix by convention.
        string typeName = typeof(TRequest).Name;
        if (!typeName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
        {
            return await next();
        }

        logger.LogInformation(
            "AUDIT Command={CommandName} User={UserId} At={Timestamp}",
            typeName,
            currentUser.UserIdOrAnonymous,
            DateTimeOffset.UtcNow);

        return await next();
    }
}


