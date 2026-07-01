using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class SaleService : ISaleService
{
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<HeldSale> _heldSaleRepository;
    private readonly IStoreContext _storeContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(
        IRepository<Sale> saleRepository,
        IRepository<HeldSale> heldSaleRepository,
        IStoreContext storeContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _heldSaleRepository = heldSaleRepository;
        _storeContext = storeContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var sale = new Sale
        {
            Number = GenerateSaleNumber(),
            Date = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            UserId = _currentUserService.UserId ?? "system",
            ShiftId = request.ShiftId,
            StoreId = request.StoreId,
            SubTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice),
            TaxAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice * (i.TaxRate / 100m)),
            DiscountAmount = request.Items.Sum(i => i.Discount),
            TotalAmount = request.Items.Sum(i => (i.Quantity * i.UnitPrice) - i.Discount + ((i.Quantity * i.UnitPrice) * (i.TaxRate / 100m))),
            PaidAmount = request.Payments.Sum(p => p.Amount),
            ChangeAmount = 0,
            LoyaltyPointsRedeemed = request.LoyaltyPointsRedeemed ?? 0,
            Status = SaleStatus.Completed,
            Notes = null
        };

        sale.ChangeAmount = Math.Max(0, sale.PaidAmount - sale.TotalAmount);

        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(sale);
    }

    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        return sale == null ? null : MapToDto(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.FindAsync(s => s.ShiftId == shiftId, cancellationToken);
        return sales.Select(MapToDto);
    }

    public async Task<HeldSaleDto> HoldSaleAsync(HoldSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (_storeContext.GetCurrentStoreId() != request.StoreId)
            throw new InvalidOperationException("Store context mismatch");

        var heldSale = new HeldSale
        {
            UserId = _currentUserService.UserId ?? "system",
            StoreId = request.StoreId,
            CustomerId = request.CustomerId,
            SerializedItems = request.SerializedItems,
            Notes = null,
            HeldAt = DateTime.UtcNow
        };

        await _heldSaleRepository.AddAsync(heldSale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new HeldSaleDto(heldSale.Id, heldSale.HeldAt, heldSale.CustomerId?.ToString(), heldSale.SerializedItems);
    }

    public async Task<IEnumerable<HeldSaleDto>> GetHeldSalesAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var heldSales = await _heldSaleRepository.FindAsync(h => h.StoreId == storeId, cancellationToken);
        return heldSales.Select(h => new HeldSaleDto(h.Id, h.HeldAt, h.CustomerId?.ToString(), h.SerializedItems));
    }

    public async Task<bool> ResumeHeldSaleAsync(Guid heldSaleId, CancellationToken cancellationToken = default)
    {
        var heldSale = await _heldSaleRepository.GetByIdAsync(heldSaleId, cancellationToken);
        if (heldSale == null)
            return false;

        _heldSaleRepository.Remove(heldSale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> VoidSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId, cancellationToken);
        if (sale == null)
            return false;

        sale.Status = SaleStatus.Voided;
        sale.Notes = reason;
        _saleRepository.Update(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RefundSaleAsync(Guid saleId, string reason, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId, cancellationToken);
        if (sale == null)
            return false;

        sale.Status = SaleStatus.Refunded;
        sale.Notes = reason;
        _saleRepository.Update(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private SaleDto MapToDto(Sale sale) => new SaleDto(
        sale.Id,
        sale.Number,
        sale.Date,
        sale.CustomerId,
        sale.CustomerId.HasValue ? sale.CustomerId.Value.ToString() : null,
        sale.SubTotal,
        sale.TaxAmount,
        sale.DiscountAmount,
        sale.TotalAmount,
        sale.PaidAmount,
        sale.ChangeAmount,
        sale.Status,
        sale.Items.Select(i => new SaleItemDto(
            i.ProductId,
            i.Product?.Name ?? "Unknown",
            i.Quantity,
            i.UnitPrice,
            0,
            i.TaxRate,
            i.Quantity * i.UnitPrice)),
        sale.Payments.Select(p => new SalePaymentDto(p.Method, p.Amount, p.PaidAt)));

    private string GenerateSaleNumber() => $"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
