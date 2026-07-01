using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Interfaces;

public interface IInventoryCountService
{
    Task<InventoryCountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryCountDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryCountDto>> GetByStatusAsync(InventoryCountStatus status, CancellationToken cancellationToken = default);
    Task<InventoryCountDto> CreateAsync(CreateInventoryCountRequest request, CancellationToken cancellationToken = default);
    Task<InventoryCountDto> AddItemAsync(Guid countId, AddInventoryCountItemRequest item, CancellationToken cancellationToken = default);
    Task<InventoryCountDto> UpdateAsync(UpdateInventoryCountRequest request, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record InventoryCountDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    InventoryCountStatus Status,
    DateTime CountDate,
    int TotalItemsCounted,
    int ItemsWithVariance,
    IEnumerable<InventoryCountItemDetailDto> Items);

public record InventoryCountItemDetailDto(
    Guid ProductId,
    string ProductName,
    decimal SystemQuantity,
    decimal? ActualQuantity,
    decimal? Variance,
    string? Notes);

public record CreateInventoryCountRequest(
    Guid WarehouseId,
    DateTime CountDate,
    string? Notes = null);

public record AddInventoryCountItemRequest(
    Guid ProductId,
    decimal ActualQuantity,
    string? Notes = null);

public record UpdateInventoryCountRequest(
    Guid Id,
    string? Notes);
