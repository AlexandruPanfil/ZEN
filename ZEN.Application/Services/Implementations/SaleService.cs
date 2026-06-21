using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class SaleService : ISaleService
{
    // TODO: Implement full sale processing logic
    public Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SaleDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<HeldSaleDto> HoldSaleAsync(HoldSaleRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<HeldSaleDto>> GetHeldSalesAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResumeHeldSaleAsync(Guid heldSaleId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> VoidSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RefundSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
