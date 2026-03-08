using System.Security.Claims;
using BaustellenBob.Shared.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace BaustellenBob.Server.Services;

/// <summary>
/// Resolves TenantId from the authenticated user's claims.
/// </summary>
public class TenantProvider : ITenantProvider
{
    private readonly AuthenticationStateProvider _authState;

    public TenantProvider(AuthenticationStateProvider authState)
    {
        _authState = authState;
    }

    public Guid TenantId
    {
        get
        {
            var state = _authState.GetAuthenticationStateAsync().GetAwaiter().GetResult();
            var claim = state.User.FindFirst("TenantId");
            return claim is not null ? Guid.Parse(claim.Value) : Guid.Empty;
        }
    }
}
