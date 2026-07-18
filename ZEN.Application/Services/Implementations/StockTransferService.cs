using ZEN.Application.DTOs.Inventory;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class StockTransferService : IStockTransferService
{
    private readonly IRepository<StockTransfer> _transferRepository;
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<InventoryItem> _inventoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public StockTransferService(
        IRepository<StockTransfer> transferRepository,
        IRepository<Warehouse> warehouseRepository,
        IRepository<Product> productRepository,
        IRepository<InventoryItem> inventoryRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _transferRepository = transferRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<StockTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        return transfer == null ? null : await MapToDtoAsync(transfer, cancellationToken);
    }

    public async Task<IEnumerable<StockTransferDto>> GetBySourceWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.FindAsync(t => t.SourceWarehouseId == warehouseId, cancellationToken);
        var result = new List<StockTransferDto>();
        foreach (var transfer in transfers)
            result.Add(await MapToDtoAsync(transfer, cancellationToken));
        return result;
    }

    public async Task<IEnumerable<StockTransferDto>> GetByDestinationWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.FindAsync(t => t.DestinationWarehouseId == warehouseId, cancellationToken);
        var result = new List<StockTransferDto>();
        foreach (var transfer in transfers)
            result.Add(await MapToDtoAsync(transfer, cancellationToken));
        return result;
    }

    public async Task<IEnumerable<StockTransferDto>> GetByStatusAsync(StockTransferStatus status, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.FindAsync(t => t.Status == status, cancellationToken);
        var result = new List<StockTransferDto>();
        foreach (var transfer in transfers)
            result.Add(await MapToDtoAsync(transfer, cancellationToken));
        return result;
    }

    public async Task<StockTransferDto> CreateAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var sourceWh = await _warehouseRepository.GetByIdAsync(request.SourceWarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Source warehouse not found");
        var destWh = await _warehouseRepository.GetByIdAsync(request.DestinationWarehouseId, cancellationToken)
            ?? throw new KeyNotFoundException("Destination warehouse not found");

        if (sourceWh.StoreId != destWh.StoreId)
            throw new InvalidOperationException("Warehouses must be in the same store");

        var transfer = new StockTransfer
        {
            Number = GenerateTransferNumber(),
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            TransferDate = request.TransferDate,
            Status = StockTransferStatus.Draft,
            Notes = request.Notes,
            UserId = _currentUserService.UserId ?? "system"
        };

        foreach (var itemRequest in request.Items)
        {
            var inventory = await _inventoryRepository.FindFirstAsync(i => i.ProductId == itemRequest.ProductId && i.WarehouseId == request.SourceWarehouseId, cancellationToken);
            if (inventory == null || inventory.Quantity < itemRequest.Quantity)
                throw new InvalidOperationException($"Insufficient inventory for product {itemRequest.ProductId}");

            var item = new StockTransferItem
            {
                ProductId = itemRequest.ProductId,
                Quantity = itemRequest.Quantity
            };
            transfer.Items.Add(item);
        }

        await _transferRepository.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(transfer, cancellationToken);
    }

    public async Task<StockTransferDto> UpdateAsync(UpdateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Stock transfer not found");

        if (transfer.Status != StockTransferStatus.Draft)
            throw new InvalidOperationException("Only draft transfers can be updated");

        transfer.TransferDate = request.TransferDate;
        transfer.Notes = request.Notes;

        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapToDtoAsync(transfer, cancellationToken);
    }

    public async Task<bool> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
            return false;

        if (transfer.Status != StockTransferStatus.Draft)
            throw new InvalidOperationException("Only draft transfers can be approved");

        transfer.Status = StockTransferStatus.InTransit;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReceiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
            return false;

        if (transfer.Status != StockTransferStatus.InTransit)
            throw new InvalidOperationException("Only in-transit transfers can be received");

        foreach (var item in transfer.Items)
        {
            var sourceInventory = await _inventoryRepository.FindFirstAsync(i => i.ProductId == item.ProductId && i.WarehouseId == transfer.SourceWarehouseId, cancellationToken);
            if (sourceInventory != null)
            {
                sourceInventory.Quantity -= item.Quantity;
                _inventoryRepository.Update(sourceInventory);
            }

            var destinationInventory = await _inventoryRepository.FindFirstAsync(i => i.ProductId == item.ProductId && i.WarehouseId == transfer.DestinationWarehouseId, cancellationToken);
            if (destinationInventory != null)
            {
                destinationInventory.Quantity += item.Quantity;
                _inventoryRepository.Update(destinationInventory);
            }
            else
            {
                await _inventoryRepository.AddAsync(new InventoryItem
                {
                    ProductId = item.ProductId,
                    WarehouseId = transfer.DestinationWarehouseId,
                    Quantity = item.Quantity
                }, cancellationToken);
            }
        }

        transfer.Status = StockTransferStatus.Received;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
            return false;

        if (transfer.Status == StockTransferStatus.Received)
            throw new InvalidOperationException("Cannot cancel a received transfer");

        transfer.Status = StockTransferStatus.Cancelled;
        _transferRepository.Update(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetByIdAsync(id, cancellationToken);
        if (transfer == null)
            return false;

        if (transfer.Status != StockTransferStatus.Draft)
            throw new InvalidOperationException("Only draft transfers can be deleted");

        _transferRepository.Remove(transfer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<StockTransferDto> MapToDtoAsync(StockTransfer transfer, CancellationToken cancellationToken)
    {
        var source = await _warehouseRepository.GetByIdAsync(transfer.SourceWarehouseId, cancellationToken);
        var destination = await _warehouseRepository.GetByIdAsync(transfer.DestinationWarehouseId, cancellationToken);

        var items = new List<StockTransferItemDto>();
        foreach (var item in transfer.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            items.Add(new StockTransferItemDto(item.ProductId, product?.Name ?? "Unknown", item.Quantity));
        }

        return new StockTransferDto(
            transfer.Id,
            transfer.Number,
            transfer.TransferDate,
            transfer.SourceWarehouseId,
            source?.Name ?? "Unknown",
            transfer.DestinationWarehouseId,
            destination?.Name ?? "Unknown",
            source?.StoreId ?? Guid.Empty,
            source?.Store?.Name ?? "Unknown",
            transfer.Status,
            items);
    }

    private string GenerateTransferNumber() => $"ST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
