namespace ZEN.Application.Services.Interfaces;

public interface IStoreContext
{
    Guid? GetCurrentStoreId();
    Guid? GetCurrentWarehouseId();
    Task SetCurrentStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task SetCurrentWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<string?> GetCurrentStoreNameAsync(CancellationToken cancellationToken = default);
    Task<string?> GetCurrentWarehouseNameAsync(CancellationToken cancellationToken = default);
    Task<bool> CanAccessStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
