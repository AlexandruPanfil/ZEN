using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class WarehouseService : IWarehouseService
{
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseService(
        IRepository<Warehouse> warehouseRepository,
        IRepository<Store> storeRepository,
        IUnitOfWork unitOfWork)
    {
        _warehouseRepository = warehouseRepository;
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<IEnumerable<WarehouseDto>> GetByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var warehouses = await _warehouseRepository.FindAsync(w => w.StoreId == storeId, cancellationToken);

        return warehouses.Select(MapToDto);
    }

    public async Task<IEnumerable<WarehouseDto>> GetActiveByStoreAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var warehouses = await _warehouseRepository.FindAsync(w => w.StoreId == storeId && w.IsActive, cancellationToken);

        return warehouses.Select(MapToDto);
    }

    public async Task<WarehouseDto?> GetMainWarehouseAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var mainWarehouse = await _warehouseRepository.FindFirstAsync(w => w.StoreId == storeId && w.IsMain, cancellationToken);

        return mainWarehouse == null ? null : MapToDto(mainWarehouse);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(request.StoreId, cancellationToken);
        if (store == null)
            throw new KeyNotFoundException("Store not found");

        if (request.IsMain)
        {
            var existingMain = await _warehouseRepository
                .FindFirstAsync(w => w.StoreId == request.StoreId && w.IsMain, cancellationToken);
            if (existingMain != null)
            {
                existingMain.IsMain = false;
                _warehouseRepository.Update(existingMain);
            }
        }

        var warehouse = new Warehouse
        {
            Code = request.Code,
            Name = request.Name,
            Address = request.Address,
            StoreId = request.StoreId,
            IsMain = request.IsMain,
            IsActive = request.IsActive
        };

        await _warehouseRepository.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        warehouse.Store = store;
        return MapToDto(warehouse);
    }

    public async Task<WarehouseDto> UpdateAsync(UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(request.Id, cancellationToken);

        if (warehouse == null)
            throw new KeyNotFoundException("Warehouse not found");

        warehouse.Code = request.Code;
        warehouse.Name = request.Name;
        warehouse.Address = request.Address;
        warehouse.IsActive = request.IsActive;

        if (request.IsMain && !warehouse.IsMain)
        {
            var existingMain = await _warehouseRepository
                .FindFirstAsync(w => w.StoreId == warehouse.StoreId && w.IsMain && w.Id != request.Id, cancellationToken);
            if (existingMain != null)
            {
                existingMain.IsMain = false;
                _warehouseRepository.Update(existingMain);
            }
            warehouse.IsMain = true;
        }
        else if (!request.IsMain && warehouse.IsMain)
        {
            warehouse.IsMain = false;
        }

        _warehouseRepository.Update(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(warehouse);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id, cancellationToken);
        if (warehouse == null)
            return false;

        _warehouseRepository.Remove(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private WarehouseDto MapToDto(Warehouse warehouse) => new WarehouseDto(
        warehouse.Id,
        warehouse.Code,
        warehouse.Name,
        warehouse.Address,
        warehouse.StoreId,
        warehouse.Store?.Name ?? "Unknown Store",
        warehouse.IsMain,
        warehouse.IsActive);
}
