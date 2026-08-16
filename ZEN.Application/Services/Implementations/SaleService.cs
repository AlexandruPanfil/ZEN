using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ZEN.Application.Services.Implementations;

public class SaleService : ISaleService
{
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<HeldSale> _heldSaleRepository;
    private readonly IRepository<CashDesk> _cashDeskRepository;
    private readonly IRepository<Printer> _printerRepository;
    private readonly IStoreContext _storeContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFiscalCloudService _fiscalCloudService;
    private readonly IFiscalReceiptService _fiscalReceiptService;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        IRepository<Sale> saleRepository,
        IRepository<HeldSale> heldSaleRepository,
        IRepository<CashDesk> cashDeskRepository,
        IRepository<Printer> printerRepository,
        IStoreContext storeContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IFiscalCloudService fiscalCloudService,
        IFiscalReceiptService fiscalReceiptService,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _heldSaleRepository = heldSaleRepository;
        _cashDeskRepository = cashDeskRepository;
        _printerRepository = printerRepository;
        _storeContext = storeContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _fiscalCloudService = fiscalCloudService;
        _fiscalReceiptService = fiscalReceiptService;
        _logger = logger;
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

        // Add sale items
        foreach (var item in request.Items)
        {
            var taxAmount = (item.Quantity * item.UnitPrice) * (item.TaxRate / 100m);
            var totalPrice = (item.Quantity * item.UnitPrice) - item.Discount + taxAmount;
            sale.Items.Add(new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = item.TaxRate,
                TaxAmount = taxAmount,
                DiscountAmount = item.Discount,
                TotalPrice = totalPrice,
                CostPrice = 0
            });
        }

        // Add sale payments
        foreach (var payment in request.Payments)
        {
            sale.Payments.Add(new SalePayment
            {
                Method = payment.Method,
                Amount = payment.Amount,
                Reference = null
            });
        }

        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send to FiscalCloud if cash desk has fiscal printer
        if (request.CashDeskId.HasValue)
        {
            try
            {
                await SendToFiscalCloudAsync(sale, request.CashDeskId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send sale {SaleId} to FiscalCloud", sale.Id);
                // Continue - sale is saved, fiscal receipt failure is logged
            }
        }

        return MapToDto(sale);
    }

    private async Task SendToFiscalCloudAsync(Sale sale, Guid cashDeskId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking FiscalCloud integration for CashDesk {CashDeskId}", cashDeskId);

        var cashDesk = await _cashDeskRepository.GetByIdAsync(cashDeskId, cancellationToken);
        if (cashDesk?.FiscalPrinterId == null)
        {
            _logger.LogInformation("CashDesk {CashDeskId} has no fiscal printer attached", cashDeskId);
            return;
        }

        _logger.LogInformation("CashDesk has fiscal printer {PrinterId}", cashDesk.FiscalPrinterId.Value);

        var printer = await _printerRepository.GetByIdAsync(cashDesk.FiscalPrinterId.Value, cancellationToken);
        if (printer == null || printer.Type != PrinterType.Fiscal)
        {
            _logger.LogWarning("Printer {PrinterId} not found or is not a fiscal printer (Type: {Type})", 
                cashDesk.FiscalPrinterId.Value, printer?.Type);
            return;
        }

        if (string.IsNullOrEmpty(printer.ApiKey) || string.IsNullOrEmpty(printer.ApiSecret) || string.IsNullOrEmpty(printer.DeviceId))
        {
            _logger.LogWarning("Fiscal printer {PrinterId} is not configured with FiscalCloud credentials (ApiKey: {HasKey}, ApiSecret: {HasSecret}, DeviceId: {HasDevice})", 
                printer.Id, 
                !string.IsNullOrEmpty(printer.ApiKey),
                !string.IsNullOrEmpty(printer.ApiSecret),
                !string.IsNullOrEmpty(printer.DeviceId));
            return;
        }

        _logger.LogInformation("Sending sale {SaleId} to FiscalCloud via printer {PrinterId}", sale.Id, printer.Id);

        // Build FiscalCloud sale request
        var fiscalItems = sale.Items.Select(item => new Application.DTOs.FiscalCloud.FiscalCloudItemDto(
            GoodId: item.ProductId,
            Name: $"Product {item.ProductId}", // TODO: Load product name from repository
            Quantity: item.Quantity,
            Price: item.UnitPrice,
            ModifierMode: item.DiscountAmount > 0 ? "0" : null,
            ModifierValue: item.DiscountAmount > 0 ? item.DiscountAmount : null,
            TaxGroupCode: MapTaxRateToTaxGroup(item.TaxRate),
            Comment: null
        )).ToList();

        var fiscalPayments = sale.Payments.Select(payment => new Application.DTOs.FiscalCloud.FiscalCloudPaymentDto(
            TypeCode: MapPaymentMethodToTypeCode(payment.Method),
            Amount: payment.Amount,
            UseBankTerminal: null,
            BankTerminal: null,
            BankTerminalCurrency: null,
            BankTerminalRRN: null,
            BankTerminalOperationNumber: null
        )).ToList();

        var fiscalRequest = new Application.DTOs.FiscalCloud.FiscalCloudSaleRequestDto(
            Print: true,
            PrintOnServer: false,
            PrintFormMediaType: null,
            SendToEmail: null,
            EmailAddress: null,
            SendToPhone: null,
            PhoneNumber: null,
            Comment: null,
            Items: fiscalItems,
            Payments: fiscalPayments,
            ReceiptMode: null,
            AlternativeReceiptSeriesAndNumber: null
        );

        var response = await _fiscalCloudService.SendSaleAsync(printer, fiscalRequest, cancellationToken);

        // Save fiscal receipt
        var fiscalReceipt = new FiscalReceipt
        {
            SaleId = sale.Id,
            FiscalReceiptNumber = response.FiscalReceipt?.ReceiptNumber,
            FiscalReceiptDate = response.FiscalReceipt?.ReceiptDate,
            OperationType = FiscalOperationType.Sale,
            RequestJson = System.Text.Json.JsonSerializer.Serialize(fiscalRequest),
            ResponseJson = System.Text.Json.JsonSerializer.Serialize(response),
            Success = response.Success,
            ErrorMessage = response.ErrorMessage
        };

        foreach (var item in fiscalItems)
        {
            fiscalReceipt.Items.Add(new FiscalReceiptItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Price = item.Price,
                TaxGroupCode = item.TaxGroupCode,
                Total = item.Quantity * item.Price
            });
        }

        await _fiscalReceiptService.CreateAsync(fiscalReceipt, cancellationToken);

        _logger.LogInformation("Sale {SaleId} sent to FiscalCloud. Receipt: {ReceiptNumber}, Success: {Success}",
            sale.Id, fiscalReceipt.FiscalReceiptNumber, fiscalReceipt.Success);
    }

    private string MapTaxRateToTaxGroup(decimal taxRate)
    {
        // Map tax rates to FiscalCloud tax group codes
        // This should be configurable per installation
        return taxRate switch
        {
            0 => "A", // 0% - Exempt
            10 => "B", // 10% VAT
            20 => "C", // 20% VAT
            _ => "B" // Default to standard rate
        };
    }

    private int MapPaymentMethodToTypeCode(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => 0,
            PaymentMethod.CreditCard => 1,
            PaymentMethod.DebitCard => 1,
            PaymentMethod.Mobile => 2,
            _ => 0
        };
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
