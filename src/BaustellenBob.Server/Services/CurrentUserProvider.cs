using System.Security.Claims;
using BaustellenBob.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace BaustellenBob.Server.Services;

/// <summary>
/// Resolves current user info from the authenticated user's claims.
/// Uses IHttpContextAccessor for minimal API calls, falls back to
/// AuthenticationStateProvider for Blazor component scope.
/// </summary>
public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider? _authState;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider? authState = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _authState = authState;
    }

    private ClaimsPrincipal User
    {
        get
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
                return httpUser;

            if (_authState is not null)
                return _authState.GetAuthenticationStateAsync().GetAwaiter().GetResult().User;

            return new ClaimsPrincipal();
        }
    }

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
