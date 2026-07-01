using ZEN.Application.DTOs.Inventory;
using InventoryTransferDto = ZEN.Application.Services.Interfaces.StockTransferDto;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class InventoryService : IInventoryService
{
    public Task<IEnumerable<InventoryItemDto>> GetInventoryByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<InventoryItemDto?> GetInventoryByProductAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<StockPurchaseDto> CreateStockPurchaseAsync(CreateStockPurchaseRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<StockPurchaseDto>> GetStockPurchasesAsync(Guid? supplierId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<InventoryTransferDto> CreateStockTransferAsync(Guid sourceWarehouseId, Guid destinationWarehouseId, IEnumerable<(Guid ProductId, decimal Quantity)> items, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<bool> ApproveStockTransferAsync(Guid transferId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync(Guid warehouseId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
