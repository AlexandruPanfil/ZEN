using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class StoreContextService : IStoreContext
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;

    public StoreContextService(
        IRepository<Store> storeRepository,
        IRepository<Warehouse> warehouseRepository,
        ICurrentUserService currentUserService)
    {
        _storeRepository = storeRepository;
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
    }

    public Guid? GetCurrentStoreId()
    {
        return _currentUserService.StoreId;
    }

    public Guid? GetCurrentWarehouseId()
    {
        return null;
    }

    public async Task SetCurrentStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStoreAsync(storeId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User does not have access to store {storeId}");
        }
    }

    public async Task SetCurrentWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessWarehouseAsync(warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException($"User does not have access to warehouse {warehouseId}");
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
