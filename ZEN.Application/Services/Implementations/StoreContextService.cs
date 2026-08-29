using Microsoft.AspNetCore.Http;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class StoreContextService : IStoreContext
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string StoreIdCookieKey = "zen_current_store_id";
    private const string WarehouseIdCookieKey = "zen_current_warehouse_id";

    public StoreContextService(
        IRepository<Store> storeRepository,
        IRepository<Warehouse> warehouseRepository,
        ICurrentUserService currentUserService,
        IHttpContextAccessor httpContextAccessor)
    {
        _storeRepository = storeRepository;
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetCurrentStoreId()
    {
        // First check cookie for user-selected store
        var cookie = GetCookie(StoreIdCookieKey);
        if (cookie != null && Guid.TryParse(cookie, out var storeId))
        {
            return storeId;
        }

        // Fall back to user's JWT store ID (for cashiers with CashDesk)
        return _currentUserService.StoreId;
    }

    public Guid? GetCurrentWarehouseId()
    {
        var cookie = GetCookie(WarehouseIdCookieKey);
        if (cookie != null && Guid.TryParse(cookie, out var warehouseId))
        {
            return warehouseId;
        }

        return null;
    }

    public async Task SetCurrentStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStoreAsync(storeId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User does not have access to store {storeId}");
        }

        SetCookie(StoreIdCookieKey, storeId.ToString());
        RemoveCookie(WarehouseIdCookieKey); // Clear warehouse when store changes
    }

    public async Task SetCurrentWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessWarehouseAsync(warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User does not have access to warehouse {warehouseId}");
        }

        SetCookie(WarehouseIdCookieKey, warehouseId.ToString());
    }

    private string? GetCookie(string key)
    {
        try
        {
            return _httpContextAccessor.HttpContext?.Request.Cookies[key];
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void SetCookie(string key, string value)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Append(key, value, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    IsEssential = true
                });
            }
        }
        catch (InvalidOperationException)
        {
            // Response may have started, ignore
        }
    }

    private void RemoveCookie(string key)
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                context.Response.Cookies.Delete(key);
            }
        }
        catch (InvalidOperationException)
        {
            // Response may have started, ignore
        }
    }

    public async Task<string?> GetCurrentStoreNameAsync(CancellationToken cancellationToken = default)
    {
        var storeId = GetCurrentStoreId();
        if (storeId == null) return null;

        var store = await _storeRepository.GetByIdAsync(storeId.Value, cancellationToken);
        return store?.Name;
    }

    public async Task<string?> GetCurrentWarehouseNameAsync(CancellationToken cancellationToken = default)
    {
        var warehouseId = GetCurrentWarehouseId();
        if (warehouseId == null) return null;

        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId.Value, cancellationToken);
        return warehouse?.Name;
    }

    public async Task<bool> CanAccessStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(storeId, cancellationToken);
        if (store == null || !store.IsActive) return false;

        if (_currentUserService.Roles.Contains("Administrator"))
            return true;

        if (_currentUserService.StoreId.HasValue && _currentUserService.StoreId.Value == storeId)
            return true;

        return false;
    }

    public async Task<bool> CanAccessWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId, cancellationToken);
        if (warehouse == null || !warehouse.IsActive) return false;

        return await CanAccessStoreAsync(warehouse.StoreId, cancellationToken);
    }
}
