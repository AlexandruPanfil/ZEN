using ZEN.Application.DTOs.Sales;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;
using System.Linq.Expressions;

namespace ZEN.Application.Services.Implementations;

public class SellOrderService : ISellOrderService
{
    private readonly IRepository<SellOrder> _sellOrderRepository;
    private readonly IRepository<SellOrderItem> _sellOrderItemRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public SellOrderService(
        IRepository<SellOrder> sellOrderRepository,
        IRepository<SellOrderItem> sellOrderItemRepository,
        IRepository<Product> productRepository,
        IRepository<Customer> customerRepository,
        IRepository<Store> storeRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _sellOrderRepository = sellOrderRepository;
        _sellOrderItemRepository = sellOrderItemRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _storeRepository = storeRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SellOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var so = await _sellOrderRepository.GetByIdAsync(id, [s => s.Items], cancellationToken);

        return so == null ? null : await MapToDtoAsync(so, cancellationToken);
    }

    public async Task<IEnumerable<SellOrderDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var sos = await _sellOrderRepository.FindAsync(s => s.StoreId == storeId, cancellationToken);

        var result = new List<SellOrderDto>();
        foreach (var so in sos)
        {
            result.Add(await MapToDtoAsync(so, cancellationToken));
        }

        return result;
    }

    public async Task<IEnumerable<SellOrderDto>> GetByStatusAsync(SellOrderStatus status, CancellationToken cancellationToken = default)
    {
        var sos = await _sellOrderRepository.FindAsync(s => s.Status == status, cancellationToken);

        var result = new List<SellOrderDto>();
        foreach (var so in sos)
        {
            result.Add(await MapToDtoAsync(so, cancellationToken));
        }

        return result;
    }

    public async Task<IEnumerable<SellOrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sos = await _sellOrderRepository.GetAllAsync(cancellationToken);

        var result = new List<SellOrderDto>();
        foreach (var so in sos)
        {
            result.Add(await MapToDtoAsync(so, cancellationToken));
        }

        return result;
    }

    public async Task<SellOrderDto> CreateAsync(CreateSellOrderRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new KeyNotFoundException("Customer not found");

        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store == null)
            throw new KeyNotFoundException("Store not found");

        var so = new SellOrder
        {
            Number = GenerateSONumber(),
            CustomerId = request.CustomerId,
            StoreId = request.StoreId,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate,
            Status = SellOrderStatus.Draft,
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
            var discount = itemRequest.Quantity * itemRequest.Discount;
            var taxRate = itemRequest.TaxRate ?? product.Tax?.Rate ?? 0;
            var taxableAmount = itemTotal - discount;
            var itemTaxAmount = taxableAmount * taxRate / 100;
            
            subTotal += itemTotal;
            taxAmount += itemTaxAmount;

            var item = new SellOrderItem
            {
                ProductId = itemRequest.ProductId,
                OrderedQuantity = itemRequest.Quantity,
                FulfilledQuantity = 0,
                UnitPrice = itemRequest.UnitPrice,
                Discount = itemRequest.Discount,
                TaxRate = taxRate,
                TaxAmount = itemTaxAmount,
                Total = itemTotal - discount + itemTaxAmount
            };

            so.Items.Add(item);
        }

        so.SubTotal = subTotal;
        so.TaxAmount = taxAmount;
        so.TotalAmount = subTotal + taxAmount;

        await _sellOrderRepository.AddAsync(so, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        so.Customer = customer;
        so.Store = store;
        return await MapToDtoAsync(so, cancellationToken);
    }

    public async Task<SellOrderDto> UpdateAsync(UpdateSellOrderRequest request, CancellationToken cancellationToken = default)
    {
        var so = await _sellOrderRepository.GetByIdAsync(request.Id, cancellationToken);

        if (so == null)
            throw new KeyNotFoundException("Sell order not found");

        so.ExpectedDate = request.ExpectedDate;
        so.Notes = request.Notes;
        so.Status = request.Status;

        _sellOrderRepository.Update(so);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(so, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var so = await _sellOrderRepository.GetByIdAsync(id, cancellationToken);
        if (so == null)
            return false;

        // Only allow deletion of draft orders
        if (so.Status != SellOrderStatus.Draft)
            throw new InvalidOperationException("Only draft sell orders can be deleted");

        _sellOrderRepository.Remove(so);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> FulfillAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var so = await _sellOrderRepository.GetByIdAsync(id, cancellationToken);
        if (so == null)
            return false;

        // Check if all items are fulfilled
        bool allFulfilled = so.Items.All(i => i.FulfilledQuantity >= i.OrderedQuantity);
        
        if (allFulfilled)
        {
            so.Status = SellOrderStatus.Fulfilled;
        }
        else
        {
            so.Status = SellOrderStatus.PartiallyFulfilled;
        }

        _sellOrderRepository.Update(so);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var so = await _sellOrderRepository.GetByIdAsync(id, cancellationToken);
        if (so == null)
            return false;

        so.Status = SellOrderStatus.Cancelled;
        _sellOrderRepository.Update(so);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<SellOrderDto> MapToDtoAsync(SellOrder so, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(so.CustomerId, cancellationToken);
        var store = await _storeRepository.GetByIdAsync(so.StoreId, cancellationToken);

        var dtoItems = new List<SellOrderItemDto>();
        foreach (var item in so.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            dtoItems.Add(new SellOrderItemDto(
                item.ProductId,
                product?.Name ?? "Unknown",
                item.OrderedQuantity,
                item.UnitPrice,
                item.Discount,
                item.TaxRate,
                item.Total));
        }

        return new SellOrderDto(
            so.Id,
            so.Number,
            so.CustomerId,
            customer?.FullName ?? "Unknown",
            so.StoreId,
            store?.Name ?? "Unknown",
            so.OrderDate,
            so.ExpectedDate,
            so.Status,
            so.SubTotal,
            so.TaxAmount,
            so.TotalAmount,
            dtoItems);
    }

    private string GenerateSONumber()
    {
        return $"SO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}