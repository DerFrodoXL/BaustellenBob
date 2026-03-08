using System.Security.Claims;
using BaustellenBob.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace BaustellenBob.Server.Services;

/// <summary>
/// Resolves current user info from the authenticated user's claims.
/// </summary>
public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly AuthenticationStateProvider _authState;

    public CurrentUserProvider(AuthenticationStateProvider authState)
    {
        _authState = authState;
    }

    private ClaimsPrincipal User =>
        _authState.GetAuthenticationStateAsync().GetAwaiter().GetResult().User;

    public Guid UserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }

    public string UserName => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    public string Role => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
}
