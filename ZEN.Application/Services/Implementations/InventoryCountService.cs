using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class InventoryCountService : IInventoryCountService
{
    private readonly IRepository<InventoryCount> _countRepository;
    private readonly IRepository<InventoryCountItem> _countItemRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryCountService(
        IRepository<InventoryCount> countRepository,
        IRepository<InventoryCountItem> countItemRepository,
        IRepository<InventoryItem> inventoryRepository,
        IRepository<Warehouse> warehouseRepository,
        IRepository<Product> productRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _countRepository = countRepository;
        _countItemRepository = countItemRepository;
        _inventoryRepository = inventoryRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<InventoryCountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var count = await _countRepository.GetByIdAsync(id, cancellationToken);
        return count == null ? null : await MapToDtoAsync(count, cancellationToken);
    }

    public async Task<IEnumerable<InventoryCountDto>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var counts = await _countRepository.FindAsync(c => c.WarehouseId == warehouseId, cancellationToken);
        var result = new List<InventoryCountDto>();
        foreach (var count in counts)
        {
            result.Add(await MapToDtoAsync(count, cancellationToken));
        }
        return result;
    }

    public async Task<IEnumerable<InventoryCountDto>> GetByStatusAsync(InventoryCountStatus status, CancellationToken cancellationToken = default)
    {
        var counts = await _countRepository.FindAsync(c => c.Status == status, cancellationToken);
        var result = new List<InventoryCountDto>();
        foreach (var count in counts)
        {
            result.Add(await MapToDtoAsync(count, cancellationToken));
        }
        return result;
    }

    public async Task<InventoryCountDto> CreateAsync(CreateInventoryCountRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse == null)
            throw new KeyNotFoundException("Warehouse not found");

        var count = new InventoryCount
        {
            WarehouseId = request.WarehouseId,
            StartDate = request.CountDate,
            Status = InventoryCountStatus.Draft,
            Notes = request.Notes,
            UserId = _currentUserService.UserId ?? "system"
        };

        await _countRepository.AddAsync(count, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(count, cancellationToken);
    }

    public async Task<InventoryCountDto> AddItemAsync(Guid countId, AddInventoryCountItemRequest item, CancellationToken cancellationToken = default)
    {
        var count = await _countRepository.GetByIdAsync(countId, cancellationToken);
        if (count == null)
            throw new KeyNotFoundException("Inventory count not found");

        if (count.Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Can only add items to draft counts");

        var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
        if (product == null)
            throw new KeyNotFoundException("Product not found");

        var existing = await _countItemRepository.FindFirstAsync(x => x.InventoryCountId == countId && x.ProductId == item.ProductId, cancellationToken);
        if (existing != null)
        {
            existing.CountedQuantity = item.ActualQuantity;
            _countItemRepository.Update(existing);
        }
        else
        {
            var countItem = new InventoryCountItem
            {
                InventoryCountId = countId,
                ProductId = item.ProductId,
                ExpectedQuantity = await GetExpectedQuantityAsync(count.WarehouseId, item.ProductId, cancellationToken),
                CountedQuantity = item.ActualQuantity
            };

            await _countItemRepository.AddAsync(countItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(count, cancellationToken);
    }

    public async Task<InventoryCountDto> UpdateAsync(UpdateInventoryCountRequest request, CancellationToken cancellationToken = default)
    {
        var count = await _countRepository.GetByIdAsync(request.Id, cancellationToken);
        if (count == null)
            throw new KeyNotFoundException("Inventory count not found");

        if (count.Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Can only update draft counts");

        count.Notes = request.Notes;
        _countRepository.Update(count);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(count, cancellationToken);
    }

    public async Task<bool> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var count = await _countRepository.GetByIdAsync(id, cancellationToken);
        if (count == null)
            return false;

        if (count.Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Only draft counts can be completed");

        var items = await _countItemRepository.FindAsync(x => x.InventoryCountId == id, cancellationToken);
        foreach (var item in items)
        {
            var inventory = await _inventoryRepository.FindFirstAsync(i => i.WarehouseId == count.WarehouseId && i.ProductId == item.ProductId, cancellationToken);
            if (inventory != null)
            {
                inventory.Quantity = item.CountedQuantity;
                _inventoryRepository.Update(inventory);
            }
        }

        count.Status = InventoryCountStatus.Completed;
        count.EndDate = DateTime.UtcNow;
        _countRepository.Update(count);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var count = await _countRepository.GetByIdAsync(id, cancellationToken);
        if (count == null)
            return false;

        if (count.Status != InventoryCountStatus.Draft)
            throw new InvalidOperationException("Only draft counts can be deleted");

        var items = await _countItemRepository.FindAsync(x => x.InventoryCountId == id, cancellationToken);
        if (items.Count > 0)
            _countItemRepository.RemoveRange(items);

        _countRepository.Remove(count);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<InventoryCountDto> MapToDtoAsync(InventoryCount count, CancellationToken cancellationToken)
    {
        var items = await _countItemRepository.FindAsync(x => x.InventoryCountId == count.Id, cancellationToken);
        var result = new List<InventoryCountItemDetailDto>();
        var varianceItems = 0;

        foreach (var item in items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            result.Add(new InventoryCountItemDetailDto(
                item.ProductId,
                product?.Name ?? "Unknown",
                item.ExpectedQuantity,
                item.CountedQuantity,
                item.Discrepancy,
                null));

            if (item.Discrepancy != 0)
                varianceItems++;
        }

        var warehouse = await _warehouseRepository.GetByIdAsync(count.WarehouseId, cancellationToken);
        return new InventoryCountDto(
            count.Id,
            count.WarehouseId,
            warehouse?.Name ?? "Unknown",
            count.Status,
            count.StartDate,
            items.Count,
            varianceItems,
            result);
    }

    private async Task<decimal> GetExpectedQuantityAsync(Guid warehouseId, Guid productId, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.FindFirstAsync(i => i.WarehouseId == warehouseId && i.ProductId == productId, cancellationToken);
        return inventory?.Quantity ?? 0;
    }
}
