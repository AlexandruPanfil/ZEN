using System.Security.Claims;
using ZEN.Domain.Interfaces;
using ZEN.Windows.Services;

namespace ZEN.Windows.Services;

/// <summary>
/// CurrentUserService implementation for MAUI Windows app that uses PosAuthState
/// instead of HttpContext (which is unavailable in MAUI Blazor).
/// </summary>
public class PosCurrentUserService : ICurrentUserService
{
    private readonly PosAuthState _authState;

    public PosCurrentUserService(PosAuthState authState)
    {
        _authState = authState;
    }

    public string? UserId => _authState.UserName ?? string.Empty;

    public string? UserName => _authState.UserName;

    public Guid? StoreId => _authState.StoreId;

    public IEnumerable<string> Roles => !string.IsNullOrEmpty(_authState.Role) ? [_authState.Role] : [];

    public bool IsAuthenticated => _authState.IsAuthenticated;

    public string? IpAddress => null; // MAUI client doesn't expose IP in same way
}