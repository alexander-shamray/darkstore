using DarkStore.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DarkStore.API.Middleware;

/// <summary>
/// Centralized exception handler — converts all unhandled exceptions to
/// RFC 7807 ProblemDetails responses with a correlation-ID for tracing.
///
/// Fault-tolerance role:
///   • Prevents raw stack-traces leaking to clients
///   • Maps Application ValidationException (FluentValidation failures) → 422 Unprocessable Entity
///   • Maps domain InvalidOperationException (state-machine violations) → 409 Conflict
///   • Maps ArgumentException / ArgumentNullException → 400 Bad Request
///   • Maps KeyNotFoundException → 404 Not Found
///   • Everything else → 500 Internal Server Error
///   • Structured log entry with CorrelationId + exception details on every fault
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        string correlationId = httpContext.TraceIdentifier;

        // Validation errors → 422 with per-field error dictionary (RFC 7807 "errors" extension)
        if (exception is ValidationException validationEx)
        {
            logger.LogWarning(
                "Validation failed — CorrelationId: {CorrelationId} — {ErrorCount} error(s)",
                correlationId, validationEx.Errors.Count);

            var validationProblem = new ProblemDetails
            {
                Status   = StatusCodes.Status422UnprocessableEntity,
                Title    = "Validation Error",
                Detail   = validationEx.Message,
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["correlationId"] = correlationId,
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["errors"] = validationEx.Errors
                }
            };

            httpContext.Response.StatusCode  = StatusCodes.Status422UnprocessableEntity;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);
            return true;
        }

        (int statusCode, string title) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status409Conflict,          "Business Rule Violation"),
            ArgumentNullException     => (StatusCodes.Status400BadRequest,         "Bad Request"),
            ArgumentException         => (StatusCodes.Status400BadRequest,         "Bad Request"),
            KeyNotFoundException      => (StatusCodes.Status404NotFound,           "Resource Not Found"),
            TimeoutException          => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable"),
            _                         => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        logger.LogError(
            exception,
            "Unhandled exception [{ExceptionType}] — CorrelationId: {CorrelationId} — {Message}",
            exception.GetType().Name,
            correlationId,
            exception.Message);

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true; // exception has been handled
    }
}
