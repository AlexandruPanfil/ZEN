using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ZEN.Domain.Interfaces;

namespace ZEN.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

public Guid? StoreId => 
        Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue("store_id"), out var storeId) 
            ? storeId : null;

    public Guid? CashDeskId => 
        Guid.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirstValue("cash_desk_id"), out var cashDeskId) 
            ? cashDeskId : null;

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value) ?? [];

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
