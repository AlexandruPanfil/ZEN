using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ZEN.Windows.Services;

public class PosAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly PosAuthState _posAuthState;

    public PosAuthenticationStateProvider(PosAuthState posAuthState)
    {
        _posAuthState = posAuthState;
        _posAuthState.Changed += OnPosAuthStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsIdentity identity;
        if (_posAuthState.IsAuthenticated)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, _posAuthState.UserName ?? "Unknown"),
                new Claim(ClaimTypes.Role, _posAuthState.Role ?? "Guest")
            };
            identity = new ClaimsIdentity(claims, "PosAuth");
        }
        else
        {
            identity = new ClaimsIdentity();
        }

        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    private void OnPosAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}