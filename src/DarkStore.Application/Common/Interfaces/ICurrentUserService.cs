namespace DarkStore.Application.Common.Interfaces;

/// <summary>
/// Provides the identity of the currently authenticated user extracted from the JWT token.
/// Implemented in DarkStore.API via IHttpContextAccessor so Application layer stays
/// infrastructure-agnostic (1.B — MVP).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The UserId claim from the JWT ("sub" or custom "uid" claim), or <c>null</c> for anonymous requests.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// String representation used for audit fields (CreatedBy / UpdatedBy).
    /// Returns the UserId string, or "anonymous" for unauthenticated requests.
    /// </summary>
    string UserIdOrAnonymous { get; }

    /// <summary>
    /// Returns <c>true</c> when the current request carries a valid authenticated identity.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Roles assigned to the current user extracted from JWT role claims.
    /// Empty collection for anonymous or unauthenticated requests.
    /// </summary>
    IReadOnlyList<string> Roles { get; }
}

