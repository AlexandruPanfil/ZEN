using ZEN.Application.DTOs.Sales;

namespace ZEN.Application.Services.Interfaces;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SaleDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<HeldSaleDto> HoldSaleAsync(HoldSaleRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<HeldSaleDto>> GetHeldSalesAsync(Guid? storeId, CancellationToken cancellationToken = default);
    Task<bool> ResumeHeldSaleAsync(Guid heldSaleId, CancellationToken cancellationToken = default);
    Task<bool> CompleteHeldSaleAsync(Guid heldSaleId, string completedBy, CancellationToken cancellationToken = default);
    Task<bool> VoidSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default);
    Task<bool> RefundSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default);
}
