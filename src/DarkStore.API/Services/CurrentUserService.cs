using DarkStore.Application.Common.Interfaces;
using System.Security.Claims;

namespace DarkStore.API.Services;

/// <summary>
/// Extracts the current user's identity from the JWT token via <see cref="IHttpContextAccessor"/>.
/// Registered as a Scoped service so each request gets a fresh accessor snapshot.
///
/// JWT claim mapping:
///   UserId  → "sub" standard claim (or "uid" custom claim as fallback)
///   Roles   → ClaimTypes.Role ("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            // "sub" is the standard JWT subject claim; some IdPs also use a custom "uid" claim.
            string? raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? Principal?.FindFirstValue("sub")
                          ?? Principal?.FindFirstValue("uid");

            return Guid.TryParse(raw, out Guid id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string UserIdOrAnonymous =>
        UserId?.ToString() ?? "anonymous";

    /// <inheritdoc />
    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles =>
        Principal?
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList()
        ?? [];
}

