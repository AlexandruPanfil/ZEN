using ZEN.Application.DTOs.Inventory;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class InventoryService : IInventoryService
{
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<StockPurchase> _stockPurchaseRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IRepository<InventoryItem> inventoryRepository,
        IRepository<Warehouse> warehouseRepository,
        IRepository<Product> productRepository,
        IRepository<StockPurchase> stockPurchaseRepository,
        IRepository<Supplier> supplierRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _stockPurchaseRepository = stockPurchaseRepository;
        _supplierRepository = supplierRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetInventoryByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.FindAsync(i => i.WarehouseId == warehouseId, cancellationToken);
        var result = new List<InventoryItemDto>();

        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            var warehouse = await _warehouseRepository.GetByIdAsync(item.WarehouseId, cancellationToken);
            
            result.Add(new InventoryItemDto(
                item.Id,
                item.ProductId,
                product?.Name ?? "Unknown Product",
                product?.SKU,
                product?.Barcode,
                item.WarehouseId,
                warehouse?.Name ?? "Unknown Warehouse",
                item.Quantity,
                item.MinQuantity,
                item.MaxQuantity));
        }

        return result;
    }

    public async Task<InventoryItemDto?> GetInventoryByProductAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepository.FindFirstAsync(
            i => i.ProductId == productId && i.WarehouseId == warehouseId, 
            cancellationToken);

        if (item == null) return null;

        var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
        var warehouse = await _warehouseRepository.GetByIdAsync(item.WarehouseId, cancellationToken);

        return new InventoryItemDto(
            item.Id,
            item.ProductId,
            product?.Name ?? "Unknown Product",
            product?.SKU,
            product?.Barcode,
            item.WarehouseId,
            warehouse?.Name ?? "Unknown Warehouse",
            item.Quantity,
            item.MinQuantity,
            item.MaxQuantity);
    }

    public async Task<StockPurchaseDto> CreateStockPurchaseAsync(CreateStockPurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier == null)
            throw new KeyNotFoundException("Supplier not found");

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse == null)
            throw new KeyNotFoundException("Warehouse not found");

        var stockPurchase = new StockPurchase
        {
            Number = GeneratePurchaseNumber(),
            SupplierId = request.SupplierId,
            PurchaseOrderId = request.PurchaseOrderId,
            WarehouseId = request.WarehouseId,
            ReceiptDate = request.ReceiptDate,
            SupplierInvoiceNo = request.SupplierInvoiceNo,
            PaidAmount = request.PaidAmount,
            Status = StockPurchaseStatus.Draft,
            UserId = _currentUserService.UserId ?? "system"
        };

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product {itemRequest.ProductId} not found");

            var itemTotal = itemRequest.Quantity * itemRequest.UnitCost;
            var itemTaxAmount = itemTotal * itemRequest.TaxRate / 100;
            stockPurchase.SubTotal += itemTotal;
            stockPurchase.TaxAmount += itemTaxAmount;

            var item = new StockPurchaseItem
            {
                ProductId = itemRequest.ProductId,
                Quantity = itemRequest.Quantity,
                UnitCost = itemRequest.UnitCost,
                TaxRate = itemRequest.TaxRate,
                TaxAmount = itemTaxAmount,
                TotalCost = itemTotal
            };
            stockPurchase.Items.Add(item);
        }

        stockPurchase.TotalAmount = stockPurchase.SubTotal + stockPurchase.TaxAmount;

        await _stockPurchaseRepository.AddAsync(stockPurchase, cancellationToken);

        foreach (var item in stockPurchase.Items)
        {
            var inventoryItem = await _inventoryRepository.FindFirstAsync(
                i => i.ProductId == item.ProductId && i.WarehouseId == request.WarehouseId, 
                cancellationToken);

            if (inventoryItem != null)
            {
                inventoryItem.Quantity += item.Quantity;
                _inventoryRepository.Update(inventoryItem);
            }
            else
            {
                var newInventoryItem = new InventoryItem
                {
                    ProductId = item.ProductId,
                    WarehouseId = request.WarehouseId,
                    Quantity = item.Quantity
                };
                await _inventoryRepository.AddAsync(newInventoryItem, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        stockPurchase.Supplier = supplier;
        stockPurchase.Warehouse = warehouse;
        return MapStockPurchaseToDto(stockPurchase);
    }

    public async Task<IEnumerable<StockPurchaseDto>> GetStockPurchasesAsync(Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var purchases = supplierId.HasValue
            ? await _stockPurchaseRepository.FindAsync(p => p.SupplierId == supplierId.Value, cancellationToken)
            : await _stockPurchaseRepository.GetAllAsync(cancellationToken);

        var result = new List<StockPurchaseDto>();
        foreach (var purchase in purchases)
        {
            var supplier = await _supplierRepository.GetByIdAsync(purchase.SupplierId, cancellationToken);
            var warehouse = await _warehouseRepository.GetByIdAsync(purchase.WarehouseId, cancellationToken);
            
            var dto = new StockPurchaseDto(
                purchase.Id,
                purchase.Number,
                purchase.ReceiptDate,
                purchase.SupplierId,
                supplier?.Name ?? "Unknown",
                purchase.WarehouseId,
                warehouse?.Name ?? "Unknown",
                purchase.SupplierInvoiceNo,
                purchase.SubTotal,
                purchase.TaxAmount,
                purchase.TotalAmount,
                purchase.PaidAmount,
                purchase.Status,
                purchase.Items.Select(i => new StockPurchaseItemDto(
                    i.ProductId,
                    i.Product?.Name ?? "Unknown",
                    i.Quantity,
                    i.UnitCost,
                    i.TaxRate,
                    i.TotalCost)));
            
            result.Add(dto);
        }

        return result;
    }

    private StockPurchaseDto MapStockPurchaseToDto(StockPurchase purchase)
    {
        var items = purchase.Items.Select(i => new StockPurchaseItemDto(
            i.ProductId,
            i.Product?.Name ?? "Unknown",
            i.Quantity,
            i.UnitCost,
            i.TaxRate,
            i.TotalCost));

        return new StockPurchaseDto(
            purchase.Id,
            purchase.Number,
            purchase.ReceiptDate,
            purchase.SupplierId,
            purchase.Supplier?.Name ?? "Unknown",
            purchase.WarehouseId,
            purchase.Warehouse?.Name ?? "Unknown",
            purchase.SupplierInvoiceNo,
            purchase.SubTotal,
            purchase.TaxAmount,
            purchase.TotalAmount,
            purchase.PaidAmount,
            purchase.Status,
            items);
    }

    private static string GeneratePurchaseNumber()
    {
        return $"GRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    public async Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.FindAsync(
            i => i.WarehouseId == warehouseId && i.Quantity <= i.MinQuantity, 
            cancellationToken);

        var result = new List<InventoryItemDto>();

        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            var warehouse = await _warehouseRepository.GetByIdAsync(item.WarehouseId, cancellationToken);

            result.Add(new InventoryItemDto(
                item.Id,
                item.ProductId,
                product?.Name ?? "Unknown Product",
                product?.SKU,
                product?.Barcode,
                item.WarehouseId,
                warehouse?.Name ?? "Unknown Warehouse",
                item.Quantity,
                item.MinQuantity,
                item.MaxQuantity));
        }

        return result;
    }
}
