using ZEN.Application.DTOs.Inventory;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Interfaces;

public interface IStockTransferService
{
    Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransferDto>> GetBySourceWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransferDto>> GetByDestinationWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransferDto>> GetByStatusAsync(StockTransferStatus status, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> UpdateAsync(UpdateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ReceiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
