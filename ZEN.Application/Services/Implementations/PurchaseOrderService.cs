using ZEN.Application.DTOs.Inventory;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IRepository<PurchaseOrder> _purchaseOrderRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseOrderService(
        IRepository<PurchaseOrder> purchaseOrderRepository,
        IRepository<Product> productRepository,
        IRepository<Supplier> supplierRepository,
        IRepository<Warehouse> warehouseRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _productRepository = productRepository;
        _supplierRepository = supplierRepository;
        _warehouseRepository = warehouseRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);

        return po == null ? null : await MapToDtoAsync(po, cancellationToken);
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var pos = await _purchaseOrderRepository.FindAsync(p => p.WarehouseId == warehouseId, cancellationToken);

        var result = new List<PurchaseOrderDto>();
        foreach (var po in pos)
        {
            result.Add(await MapToDtoAsync(po, cancellationToken));
        }

        return result;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetByStatusAsync(PurchaseOrderStatus status, CancellationToken cancellationToken = default)
    {
        var pos = await _purchaseOrderRepository.FindAsync(p => p.Status == status, cancellationToken);

        var result = new List<PurchaseOrderDto>();
        foreach (var po in pos)
        {
            result.Add(await MapToDtoAsync(po, cancellationToken));
        }

        return result;
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier == null)
            throw new KeyNotFoundException("Supplier not found");

        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse == null)
            throw new KeyNotFoundException("Warehouse not found");

        var po = new PurchaseOrder
        {
            Number = GeneratePONumber(),
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate,
            Status = PurchaseOrderStatus.Draft,
            Notes = request.Notes,
            UserId = _currentUserService.UserId ?? "system"
        };

        decimal subTotal = 0;
        decimal taxAmount = 0;

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, cancellationToken);
            if (product == null)
                throw new KeyNotFoundException($"Product {itemRequest.ProductId} not found");

            var itemTotal = itemRequest.Quantity * itemRequest.UnitPrice;
            subTotal += itemTotal;

            var item = new PurchaseOrderItem
            {
                ProductId = itemRequest.ProductId,
                OrderedQuantity = itemRequest.Quantity,
                ReceivedQuantity = 0,
                UnitCost = itemRequest.UnitPrice,
                TaxRate = 0,
                TaxAmount = 0,
                TotalCost = itemRequest.Quantity * itemRequest.UnitPrice
            };

            po.Items.Add(item);
        }

        po.SubTotal = subTotal;
        po.TaxAmount = taxAmount;
        po.TotalAmount = subTotal + taxAmount;

        await _purchaseOrderRepository.AddAsync(po, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        po.Supplier = supplier;
        po.Warehouse = warehouse;
        return await MapToDtoAsync(po, cancellationToken);
    }

    public async Task<PurchaseOrderDto> UpdateAsync(UpdatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdAsync(request.Id, cancellationToken);

        if (po == null)
            throw new KeyNotFoundException("Purchase order not found");

        po.ExpectedDate = request.ExpectedDate;
        po.Notes = request.Notes;
        po.Status = request.Status;

        _purchaseOrderRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(po, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
        if (po == null)
            return false;

        // Only allow deletion of draft orders
        if (po.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only draft purchase orders can be deleted");

        _purchaseOrderRepository.Remove(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
        if (po == null)
            return false;

        po.Status = PurchaseOrderStatus.Sent;
        _purchaseOrderRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var po = await _purchaseOrderRepository.GetByIdAsync(id, cancellationToken);
        if (po == null)
            return false;

        po.Status = PurchaseOrderStatus.Cancelled;
        _purchaseOrderRepository.Update(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pos = await _purchaseOrderRepository.GetAllAsync(cancellationToken);

        var result = new List<PurchaseOrderDto>();
        foreach (var po in pos)
        {
            result.Add(await MapToDtoAsync(po, cancellationToken));
        }

        return result;
    }

    private async Task<PurchaseOrderDto> MapToDtoAsync(PurchaseOrder po, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(po.SupplierId, cancellationToken);
        var warehouse = await _warehouseRepository.GetByIdAsync(po.WarehouseId, cancellationToken);

        var dtoItems = new List<PurchaseOrderItemDto>();
        foreach (var item in po.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            dtoItems.Add(new PurchaseOrderItemDto(
                item.ProductId,
                product?.Name ?? "Unknown",
                item.OrderedQuantity,
                item.UnitCost,
                item.TotalCost));
        }

        return new PurchaseOrderDto(
            po.Id,
            po.Number,
            po.SupplierId,
            supplier?.Name ?? "Unknown",
            po.WarehouseId,
            warehouse?.Name ?? "Unknown",
            po.OrderDate,
            po.ExpectedDate,
            po.Status,
            po.SubTotal,
            po.TaxAmount,
            po.TotalAmount,
            dtoItems);
    }

    private string GeneratePONumber()
    {
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}
