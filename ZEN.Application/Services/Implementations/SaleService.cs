using ZEN.Application.DTOs.Sales;
using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq;

namespace ZEN.Application.Services.Implementations;

public class SaleService : ISaleService
{
    private readonly IRepository<Sale> _saleRepository;
    private readonly IRepository<HeldSale> _heldSaleRepository;
    private readonly IRepository<CashDesk> _cashDeskRepository;
    private readonly IRepository<Printer> _printerRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Store> _storeRepository;
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
        IRepository<Product> productRepository,
        IRepository<Store> storeRepository,
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
        _productRepository = productRepository;
        _storeRepository = storeRepository;
        _storeContext = storeContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _fiscalCloudService = fiscalCloudService;
        _fiscalReceiptService = fiscalReceiptService;
        _logger = logger;
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateSaleRequestAsync(request, cancellationToken);

        // Resolve tax rates from products first
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.FindAsync(p => productIds.Contains(p.Id), cancellationToken);
        var productTaxRates = products.ToDictionary(p => p.Id, p => p.Tax?.Rate ?? 0m);

        var sale = new Sale
        {
            Number = GenerateSaleNumber(),
            Date = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            UserId = _currentUserService.UserId ?? "system",
            ShiftId = request.ShiftId,
            StoreId = request.StoreId,
            SubTotal = 0, // Will calculate after resolving tax rates
            TaxAmount = 0,
            DiscountAmount = 0,
            TotalAmount = 0,
            PaidAmount = request.Payments.Sum(p => p.Amount),
            ChangeAmount = 0,
            LoyaltyPointsRedeemed = request.LoyaltyPointsRedeemed ?? 0,
            Status = SaleStatus.Completed,
            Notes = null
        };

        // Add sale items with resolved tax rates - prices are tax-inclusive
        foreach (var item in request.Items)
        {
            var taxRate = item.TaxRate ?? productTaxRates.GetValueOrDefault(item.ProductId, 0m);
            
            // Price is tax-inclusive, so we need to extract tax
            // Tax Amount = (LineTotal * TaxRate) / (100 + TaxRate)
            var lineTotal = (item.Quantity * item.UnitPrice) - item.Discount;
            var taxAmount = taxRate > 0 ? (lineTotal * taxRate / (100m + taxRate)) : 0m;
            var netAmount = lineTotal - taxAmount;
            var totalPrice = lineTotal; // Total is the tax-inclusive price
            
            sale.Items.Add(new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                DiscountAmount = item.Discount,
                TotalPrice = totalPrice,
                CostPrice = 0
            });
        }

        // Calculate totals
        sale.SubTotal = sale.Items.Sum(i => i.Quantity * i.UnitPrice);
        sale.TaxAmount = sale.Items.Sum(i => i.TaxAmount);
        sale.DiscountAmount = sale.Items.Sum(i => i.DiscountAmount);
        sale.TotalAmount = sale.Items.Sum(i => i.TotalPrice);
        sale.ChangeAmount = Math.Max(0, sale.PaidAmount - sale.TotalAmount);

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

        bool? fiscalReceiptSent = null;
        string? fiscalReceiptError = null;

        // Send to FiscalCloud if a cash desk is associated with the sale.
        if (request.CashDeskId.HasValue)
        {
            try
            {
                var fiscalResult = await SendToFiscalCloudAsync(sale, request.CashDeskId.Value, cancellationToken);

                if (fiscalResult.Attempted)
                {
                    fiscalReceiptSent = fiscalResult.Success;
                    fiscalReceiptError = fiscalResult.Success ? null : fiscalResult.ErrorMessage;
                    
                    // If FiscalCloud was attempted but failed, we should NOT approve the sale
                    if (!fiscalResult.Success)
                    {
                        _logger.LogError("FiscalCloud integration failed for sale {SaleId}: {Error}", sale.Id, fiscalResult.ErrorMessage);
                        // Mark sale as failed due to fiscal receipt failure
                        sale.Status = SaleStatus.Failed;
                        sale.Notes = $"Fiscal receipt failed: {fiscalResult.ErrorMessage}";
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        // Return a failed sale DTO
                        return MapToDto(sale, false, fiscalResult.ErrorMessage);
                    }
                }
                else
                {
                    // FiscalCloud interaction was intentionally skipped (e.g. no printer
                    // configured). Surface the reason so the POS can notify the user.
                    fiscalReceiptSent = false;
                    fiscalReceiptError = fiscalResult.ErrorMessage ?? "FiscalCloud integration is not configured.";
                    
                    // If no fiscal printer is configured but cash desk exists, this is a configuration error
                    _logger.LogWarning("FiscalCloud integration skipped for sale {SaleId}: {Error}", sale.Id, fiscalReceiptError);
                    sale.Status = SaleStatus.Failed;
                    sale.Notes = $"Fiscal receipt not sent: {fiscalReceiptError}";
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    return MapToDto(sale, false, fiscalReceiptError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send sale {SaleId} to FiscalCloud", sale.Id);
                fiscalReceiptSent = false;
                fiscalReceiptError = ex.Message;
                
                // On exception, mark sale as failed
                sale.Status = SaleStatus.Failed;
                sale.Notes = $"Fiscal receipt error: {ex.Message}";
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                return MapToDto(sale, false, ex.Message);
            }
        }
        else
        {
            fiscalReceiptSent = false;
            fiscalReceiptError = "No cash desk associated with this sale - FiscalCloud receipt was not sent.";
            
            // If no cash desk, this is a configuration error - sale cannot be completed fiscally
            _logger.LogWarning("No cash desk for sale {SaleId}", sale.Id);
            sale.Status = SaleStatus.Failed;
            sale.Notes = fiscalReceiptError;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return MapToDto(sale, false, fiscalReceiptError);
        }

        return MapToDto(sale, fiscalReceiptSent, fiscalReceiptError);
    }

    private async Task ValidateSaleRequestAsync(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var items = request.Items?.ToList() ?? [];

        if (items.Count == 0)
            throw new InvalidOperationException("Cannot create a sale with no items.");

        // Validate Store exists (FK_Sales_Stores_StoreId)
        if (request.StoreId == Guid.Empty)
            throw new InvalidOperationException("StoreId is required. The cash desk must be assigned to a store.");

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store == null)
            throw new InvalidOperationException($"Store {request.StoreId} does not exist.");

        // Validate all products exist (FK_SaleItems_Products_ProductId)
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var existingProducts = await _productRepository.FindAsync(p => productIds.Contains(p.Id), cancellationToken);
        var existingIds = existingProducts.Select(p => p.Id).ToHashSet();

        var missing = productIds.Where(id => !existingIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"The following products do not exist in the database: {string.Join(", ", missing)}. " +
                "This usually means the POS is running with demo/unsynced products.");
        }

        // Validate CashDesk exists when provided
        if (request.CashDeskId.HasValue && request.CashDeskId.Value != Guid.Empty)
        {
            var cashDesk = await _cashDeskRepository.GetByIdAsync(request.CashDeskId.Value, cancellationToken);
            if (cashDesk == null)
                throw new InvalidOperationException($"CashDesk {request.CashDeskId.Value} does not exist.");
        }
    }

private async Task<(bool Attempted, bool Success, string? ErrorMessage)> SendToFiscalCloudAsync(Sale sale, Guid cashDeskId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Checking FiscalCloud integration for CashDesk {CashDeskId}", cashDeskId);

            var cashDesk = await _cashDeskRepository.GetByIdAsync(cashDeskId, cancellationToken);

            if (cashDesk == null)
            {
                _logger.LogWarning("CashDesk {CashDeskId} not found", cashDeskId);
                return (false, false, $"Cash desk {cashDeskId} not found");
            }

            // Resolve the fiscal printer: prefer the one linked to the cash desk,
            // otherwise fall back to the default configured fiscal printer. This keeps
            // sales flowing to FiscalCloud even if the cash desk itself isn't linked.
            Printer? printer = null;
            if (cashDesk.FiscalPrinterId != null)
            {
                printer = await _printerRepository.GetByIdAsync(cashDesk.FiscalPrinterId.Value, cancellationToken);
                if (printer != null && printer.Type != PrinterType.Fiscal)
                {
                    _logger.LogWarning("Printer {PrinterId} linked to cash desk {CashDeskId} is not a Fiscal printer (Type: {PrinterType})", 
                        printer.Id, cashDeskId, printer.Type);
                    printer = null;
                }
            }

            if (printer == null)
            {
                _logger.LogInformation("No fiscal printer linked to cash desk {CashDeskId}, searching for default fiscal printer", cashDeskId);
                printer = await _printerRepository.FindFirstAsync(
                    p => p.Type == PrinterType.Fiscal && p.IsActive, cancellationToken);
            }

            if (printer == null)
            {
                _logger.LogWarning("No fiscal printer is configured (cash desk {CashDeskId} has none linked and no default exists)", cashDeskId);
                return (false, false, "No fiscal printer is configured. Add a FiscalCloud printer in ZEN.Web and link it to this cash desk.");
            }

            _logger.LogInformation("Using fiscal printer {PrinterId} (Name: {Name}, Type: {Type}) for cash desk {CashDeskId}",
                printer.Id, printer.Name, printer.Type, cashDeskId);

            // Validate FiscalCloud credentials
            var missingCredentials = new List<string>();
            if (string.IsNullOrEmpty(printer.ApiKey)) missingCredentials.Add("ApiKey");
            if (string.IsNullOrEmpty(printer.ApiSecret)) missingCredentials.Add("ApiSecret");
            if (string.IsNullOrEmpty(printer.DeviceId)) missingCredentials.Add("DeviceId");

            if (missingCredentials.Count > 0)
            {
                var errorMsg = $"Fiscal printer {printer.Name} ({printer.Id}) is missing FiscalCloud credentials: {string.Join(", ", missingCredentials)}. Please configure them in ZEN.Web > Equipments > Printers.";
                _logger.LogWarning(errorMsg);
                return (false, false, errorMsg);
            }

            // Check for placeholder credentials
            var placeholderCredentials = new List<string>();
            if (printer.ApiKey?.Contains("YOUR_") == true) placeholderCredentials.Add("ApiKey");
            if (printer.ApiSecret?.Contains("YOUR_") == true) placeholderCredentials.Add("ApiSecret");
            if (printer.DeviceId?.Contains("YOUR_") == true) placeholderCredentials.Add("DeviceId");
            if (printer.PointOfSaleId?.Contains("YOUR_") == true) placeholderCredentials.Add("PointOfSaleId");

            if (placeholderCredentials.Count > 0)
            {
                var errorMsg = $"Fiscal printer {printer.Name} ({printer.Id}) has placeholder FiscalCloud credentials: {string.Join(", ", placeholderCredentials)}. Please replace with actual credentials from FiscalCloud portal.";
                _logger.LogWarning(errorMsg);
                return (false, false, errorMsg);
            }

            _logger.LogInformation("Sending sale {SaleId} to FiscalCloud via printer {PrinterId} (BaseUrl: {BaseUrl})", 
                sale.Id, printer.Id, printer.FiscalCloudBaseUrl ?? "http://localhost:50700 (default)");

        // Load product names for a human-readable fiscal receipt
        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _productRepository.FindAsync(p => productIds.Contains(p.Id), cancellationToken);
        var productNames = products.ToDictionary(p => p.Id, p => p.Name);
        var productSkus = products.ToDictionary(p => p.Id, p => p.SKU);
        var productBarcodes = products.ToDictionary(p => p.Id, p => p.Barcode);

        // Build FiscalCloud sale request
        var fiscalItems = sale.Items.Select(item => {
            // Validate price > 0
            if (item.UnitPrice <= 0)
            {
                throw new InvalidOperationException($"Product '{item.ProductId}' has invalid price ({item.UnitPrice}). Price must be greater than 0 for FiscalCloud.");
            }
            
            // Determine goodId - FiscalCloud requires goodId to exist in their product catalog
            // Only send goodId if the product has a SKU (which should be synced to FiscalCloud)
            string? goodId = null;
            if (productSkus.TryGetValue(item.ProductId, out var sku) && !string.IsNullOrWhiteSpace(sku))
            {
                goodId = sku;
            }
            else
            {
                _logger.LogWarning("Product {ProductId} ({ProductName}) has no SKU. FiscalCloud requires goodId to exist in catalog. Sale may fail.", 
                    item.ProductId, productNames.TryGetValue(item.ProductId, out var n) ? n : "Unknown");
            }
            
            // Determine taxGroupCode - only send if we have a standard tax rate that maps to known FiscalCloud codes
            // FiscalCloud codes: "A" = 20%, "B" = 8%, "_" = 0% (without TVA)
            // For non-standard rates, don't send taxGroupCode (it's optional in FiscalCloud API)
            string? taxGroupCode = null;
            var standardTaxGroup = MapTaxRateToTaxGroup(item.TaxRate);
            if (item.TaxRate == 0 || item.TaxRate == 8 || item.TaxRate == 20)
            {
                taxGroupCode = standardTaxGroup;
            }
            else
            {
                _logger.LogDebug("Product {ProductId} has non-standard tax rate {TaxRate}%. Not sending taxGroupCode to FiscalCloud (field is optional).", item.ProductId, item.TaxRate);
            }
            
            return new Application.DTOs.FiscalCloud.FiscalCloudItemDto(
                GoodId: goodId,
                Name: productNames.TryGetValue(item.ProductId, out var name) && !string.IsNullOrWhiteSpace(name)
                    ? name
                    : $"Product {item.ProductId}",
                Quantity: item.Quantity,
                Price: item.UnitPrice,
                ModifierMode: FiscalCloudModifierMode.ByAmount,
                ModifierValue: item.DiscountAmount > 0 ? item.DiscountAmount : 0m,
                TaxGroupCode: taxGroupCode,
                Comment: ""
            );
        }).ToList();

        var fiscalPayments = sale.Payments.Select(payment => {
            // Validate payment amount > 0
            if (payment.Amount <= 0)
            {
                throw new InvalidOperationException($"Payment amount must be greater than 0 for FiscalCloud. Got: {payment.Amount}");
            }
            
            // Map payment method to typeCode - only send if we have a valid mapping
            // FiscalCloud typeCode is optional, so we can omit it for unknown payment methods
            int? typeCode = MapPaymentMethodToTypeCode(payment.Method);
            
            return new Application.DTOs.FiscalCloud.FiscalCloudPaymentDto(
                TypeCode: typeCode,
                Amount: payment.Amount,
                UseBankTerminal: false,
                BankTerminal: null,
                BankTerminalCurrency: null,
                BankTerminalRRN: null,
                BankTerminalOperationNumber: null,
                DiscountAmount: 0
            );
        }).ToList();

        // Calculate total discount for amount modifications
        var totalDiscount = sale.Items.Sum(i => i.DiscountAmount);
        var amountModifications = new List<Application.DTOs.FiscalCloud.FiscalCloudAmountModificationDto>();
        if (totalDiscount > 0)
        {
            amountModifications.Add(new Application.DTOs.FiscalCloud.FiscalCloudAmountModificationDto(
                FiscalCloudModifierMode.ByAmount, totalDiscount));
        }
        // If no discount, send empty array (not an item with 0)

        // Generate a unique ID for the fiscal request
        var requestId = Guid.NewGuid().ToString();

        var fiscalRequest = new Application.DTOs.FiscalCloud.FiscalCloudSaleRequestDto(
            Id: requestId,
            PrintOnServer: true,
            PrintFormMediaType: null,
            PdfPrintFormSettings: null,
            ImagePrintFormSettings: null,
            Print: true,
            SendToEmail: false,
            EmailAddress: null,
            SendToPhone: false,
            PhoneNumber: null,
            AdditionalHeaderText: "",
            AdditionalFooterText: "Bonusuri acumulate: 34",
            Items: fiscalItems,
            Payments: fiscalPayments,
            AmountModifications: amountModifications,
            ReceiptMode: Application.DTOs.FiscalCloud.FiscalCloudReceiptMode.FiscalReceiptOnly,
            AlternativeReceiptSeriesAndNumber: ""
        );

        // Validate the fiscal request before sending
        ValidateFiscalCloudRequest(fiscalRequest, sale);

        var response = await _fiscalCloudService.SendSaleAsync(printer, fiscalRequest, cancellationToken);

        // Save fiscal receipt
        var receiptRef = response.FiscalReceipt ?? response.AlternativeReceipt;
        var requestJson = TruncateJson(System.Text.Json.JsonSerializer.Serialize(fiscalRequest));
        var responseJson = TruncateJson(System.Text.Json.JsonSerializer.Serialize(response));
        var errorMessage = TruncateErrorMessage(response.ErrorMessage);
        
        // Ensure error message is set when Success is false
        if (!response.Success && string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = "FiscalCloud operation failed without specific error message";
        }
        
        var fiscalReceipt = new FiscalReceipt
        {
            SaleId = sale.Id,
            FiscalReceiptNumber = receiptRef?.NumberPresentation ?? receiptRef?.Number?.ToString(),
            FiscalReceiptDate = ParseFiscalDateTime(receiptRef?.DateTimePresentation),
            OperationType = FiscalOperationType.Sale,
            RequestJson = requestJson,
            ResponseJson = responseJson,
            Success = response.Success,
            ErrorMessage = errorMessage
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

        try
        {
            await _fiscalReceiptService.CreateAsync(fiscalReceipt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save fiscal receipt for sale {SaleId}", sale.Id);
            // Don't throw - fiscal receipt persistence failure shouldn't block the sale
        }

        _logger.LogInformation("Sale {SaleId} sent to FiscalCloud. Receipt: {ReceiptNumber}, Success: {Success}",
            sale.Id, fiscalReceipt.FiscalReceiptNumber, fiscalReceipt.Success);

        return (true, fiscalReceipt.Success, fiscalReceipt.ErrorMessage);
    }

    private string MapTaxRateToTaxGroup(decimal taxRate)
    {
        // Map tax rates to FiscalCloud tax group codes
        // FiscalCloud codes: "A" = 20%, "B" = 8%, "_" = 0% (without TVA)
        return taxRate switch
        {
            0 => "_", // 0% - Without TVA
            8 => "B", // 8% VAT
            20 => "A", // 20% VAT
            _ => "B" // Default to standard rate (8%)
        };
    }

    private DateTime? ParseFiscalDateTime(string? presentation)
    {
        if (string.IsNullOrWhiteSpace(presentation))
            return null;

        // FiscalCloud returns formats like "08.09.2023 17:36:12"
        if (DateTime.TryParseExact(presentation, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        if (DateTime.TryParse(presentation, CultureInfo.InvariantCulture, DateTimeStyles.None, out var relaxed))
            return relaxed;

        return null;
    }

    private int? MapPaymentMethodToTypeCode(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.Cash => 0,
            PaymentMethod.CreditCard => 1,
            PaymentMethod.DebitCard => 1,
            PaymentMethod.Mobile => 2,
            _ => null // Unknown payment method - don't send typeCode
        };
    }

    private void ValidateFiscalCloudRequest(FiscalCloudSaleRequestDto request, Sale sale)
    {
        // Validate items
        if (request.Items == null || !request.Items.Any())
        {
            throw new InvalidOperationException("FiscalCloud request must contain at least one item.");
        }

        foreach (var item in request.Items)
        {
            // GoodId is now optional - only validate if provided
            if (!string.IsNullOrWhiteSpace(item.GoodId))
            {
                // Validate the goodId format if provided (should be UUID)
                if (!Guid.TryParse(item.GoodId, out _))
                {
                    _logger.LogWarning("Item GoodId '{GoodId}' is not a valid UUID format", item.GoodId);
                }
            }
            
            if (item.Quantity <= 0)
            {
                var itemId = item.GoodId ?? "unknown";
                throw new InvalidOperationException($"Item {itemId} has invalid quantity ({item.Quantity}). Must be greater than 0.");
            }
            
            if (item.Price <= 0)
            {
                var itemId = item.GoodId ?? "unknown";
                throw new InvalidOperationException($"Item {itemId} has invalid price ({item.Price}). Must be greater than 0.");
            }
            
            // TaxGroupCode is now optional - only validate if provided
            if (!string.IsNullOrWhiteSpace(item.TaxGroupCode))
            {
                // Validate TaxGroupCode is one of the expected values
                var validTaxGroups = new[] { "A", "B", "_" };
                if (!validTaxGroups.Contains(item.TaxGroupCode))
                {
                    _logger.LogWarning("Item {GoodId} has non-standard TaxGroupCode: {TaxGroupCode}. Expected one of: {ValidCodes}", 
                        item.GoodId ?? "unknown", item.TaxGroupCode, string.Join(", ", validTaxGroups));
                }
            }
        }

        // Validate payments
        if (request.Payments == null || !request.Payments.Any())
        {
            throw new InvalidOperationException("FiscalCloud request must contain at least one payment.");
        }

        var totalPaymentAmount = request.Payments.Sum(p => p.Amount);
        var totalItemAmount = request.Items.Sum(i => i.Quantity * i.Price);
        var totalDiscount = request.AmountModifications?.Sum(m => m.Value) ?? 0m;
        var expectedTotal = totalItemAmount - totalDiscount;

        if (Math.Abs(totalPaymentAmount - expectedTotal) > 0.01m)
        {
            _logger.LogWarning("Payment total ({PaymentTotal}) doesn't match item total minus discounts ({ExpectedTotal}) for sale {SaleId}. Difference: {Difference}",
                totalPaymentAmount, expectedTotal, sale.Id, Math.Abs(totalPaymentAmount - expectedTotal));
        }

        // Log the request for debugging
        _logger.LogDebug("FiscalCloud request validation passed for sale {SaleId}. Items: {ItemCount}, Payments: {PaymentCount}", 
            sale.Id, request.Items.Count(), request.Payments.Count());
    }

    /// <summary>
    /// Validates if a product exists in FiscalCloud catalog.
    /// This is a placeholder for future FiscalCloud catalog synchronization feature.
    /// Currently, we rely on FiscalCloud to validate the goodId during the request.
    /// </summary>
    private async Task<bool> ValidateProductInFiscalCloudCatalogAsync(string goodId, CancellationToken cancellationToken)
    {
        // TODO: Implement FiscalCloud catalog synchronization
        // This would require:
        // 1. A FiscalCloud catalog sync service that periodically fetches product catalog
        // 2. A local cache of valid goodIds
        // 3. Validation against the local cache before sending requests
        // For now, we return true and let FiscalCloud validate during the actual request
        return true;
    }

    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetByIdAsync(id, cancellationToken);
        return sale == null ? null : MapToDto(sale);
    }

    public async Task<IEnumerable<SaleDto>> GetByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.FindAsync(s => s.ShiftId == shiftId, cancellationToken);
        return sales.Select(s => MapToDto(s));
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

    private SaleDto MapToDto(Sale sale, bool? fiscalReceiptSent = null, string? fiscalReceiptError = null) => new SaleDto(
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
        fiscalReceiptSent,
        fiscalReceiptError,
        sale.Items.Select(i => new SaleItemDto(
            i.ProductId,
            i.Product?.Name ?? "Unknown",
            i.Quantity,
            i.UnitPrice,
            i.DiscountAmount,
            i.TaxRate,
            i.TotalPrice)),
        sale.Payments.Select(p => new SalePaymentDto(p.Method, p.Amount, p.PaidAt)));

    private string GenerateSaleNumber() => $"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static string? TruncateErrorMessage(string? message, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return message.Length <= maxLength
            ? message
            : message.Substring(0, maxLength - 3) + "...";
    }

    private static string? TruncateJson(string? json, int maxLength = 8000)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        return json.Length <= maxLength
            ? json
            : json.Substring(0, maxLength - 3) + "...";
    }
}
