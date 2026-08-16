using Microsoft.EntityFrameworkCore;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Infrastructure.Persistence;

namespace ZEN.Infrastructure.Services;

public class FiscalReceiptService : IFiscalReceiptService
{
    private readonly ZenDbContext _dbContext;

    public FiscalReceiptService(ZenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<FiscalReceipt>> GetAllAsync(Guid? saleId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FiscalReceipts
            .Include(fr => fr.Items)
            .Include(fr => fr.Sale)
            .AsQueryable();

        if (saleId.HasValue)
        {
            query = query.Where(fr => fr.SaleId == saleId.Value);
        }

        return await query
            .OrderByDescending(fr => fr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FiscalReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FiscalReceipts
            .Include(fr => fr.Items)
            .Include(fr => fr.Sale)
            .FirstOrDefaultAsync(fr => fr.Id == id, cancellationToken);
    }

    public async Task<FiscalReceipt> CreateAsync(FiscalReceipt fiscalReceipt, CancellationToken cancellationToken = default)
    {
        _dbContext.FiscalReceipts.Add(fiscalReceipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return fiscalReceipt;
    }
}
