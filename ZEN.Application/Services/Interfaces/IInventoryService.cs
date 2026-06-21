using ZEN.Application.DTOs.Inventory;

namespace ZEN.Application.Services.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<InventoryItemDto>> GetInventoryByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> GetInventoryByProductAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<StockPurchaseDto> CreateStockPurchaseAsync(CreateStockPurchaseRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockPurchaseDto>> GetStockPurchasesAsync(Guid? supplierId = null, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CreateStockTransferAsync(Guid sourceWarehouseId, Guid destinationWarehouseId, IEnumerable<(Guid ProductId, decimal Quantity)> items, CancellationToken cancellationToken = default);
    Task<bool> ApproveStockTransferAsync(Guid transferId, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
