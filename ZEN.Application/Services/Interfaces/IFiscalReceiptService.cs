using ZEN.Domain.Entities;

namespace ZEN.Application.Services.Interfaces;

public interface IFiscalReceiptService
{
    Task<IEnumerable<FiscalReceipt>> GetAllAsync(Guid? saleId = null, CancellationToken cancellationToken = default);
    Task<FiscalReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FiscalReceipt> CreateAsync(FiscalReceipt fiscalReceipt, CancellationToken cancellationToken = default);
}
