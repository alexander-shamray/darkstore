using FluentValidation.Results;

namespace DarkStore.Application.Common.Exceptions;

/// <summary>
/// Thrown by <see cref="Behaviors.ValidationBehavior{TRequest,TResponse}"/> when one or more
/// FluentValidation validators fail for an incoming MediatR request.
/// Mapped to HTTP 422 Unprocessable Entity by <c>GlobalExceptionHandler</c> in DarkStore.API.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Dictionary of property name → array of error messages.
    /// Serialised as RFC-7807 <c>errors</c> field in the ProblemDetails response.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

