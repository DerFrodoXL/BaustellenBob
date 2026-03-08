using System.Security.Claims;
using BaustellenBob.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace BaustellenBob.Server.Services;

/// <summary>
/// Resolves TenantId from the authenticated user's claims.
/// Uses IHttpContextAccessor for minimal API calls, falls back to
/// AuthenticationStateProvider for Blazor component scope.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider? _authState;

    public TenantProvider(IHttpContextAccessor httpContextAccessor, AuthenticationStateProvider? authState = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _authState = authState;
    }

    public Guid TenantId
    {
        get
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
            {
                var claim = httpUser.FindFirst("TenantId");
                if (claim is not null)
                    return Guid.Parse(claim.Value);
            }

            if (_authState is not null)
            {
                var state = _authState.GetAuthenticationStateAsync().GetAwaiter().GetResult();
                var claim = state.User.FindFirst("TenantId");
                if (claim is not null)
                    return Guid.Parse(claim.Value);
            }

            return Guid.Empty;
        }
    }
}
