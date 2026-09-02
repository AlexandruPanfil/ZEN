using ZEN.Application.DTOs.Sales;
using ZEN.Domain.Enums;

namespace ZEN.Application.Services.Interfaces;

public interface ISellOrderService
{
    Task<SellOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SellOrderDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SellOrderDto>> GetByStatusAsync(SellOrderStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<SellOrderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SellOrderDto> CreateAsync(CreateSellOrderRequest request, CancellationToken cancellationToken = default);
    Task<SellOrderDto> UpdateAsync(UpdateSellOrderRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> FulfillAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}