namespace ZEN.Application.Services.Interfaces;

public interface IWarehouseService
{
    Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarehouseDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WarehouseDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<WarehouseDto?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDto> UpdateAsync(UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    Guid StoreId,
    string StoreName,
    bool IsMain,
    bool IsActive);

public record CreateWarehouseRequest(
    string Code,
    string Name,
    string? Address,
    Guid StoreId,
    bool IsMain = false,
    bool IsActive = true);

public record UpdateWarehouseRequest(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    bool IsMain,
    bool IsActive);
