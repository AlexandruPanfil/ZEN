using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class StoreService : IStoreService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StoreService(IRepository<Store> storeRepository, IUnitOfWork unitOfWork)
    {
        _storeRepository = storeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(id, cancellationToken);
        if (store == null)
            return null;

        return new StoreDto(
            store.Id,
            store.Name,
            store.Code,
            store.Address,
            store.City,
            store.Phone,
            store.Email,
            store.IsActive,
            store.Version,
            store.IsDeleted);
    }

    public async Task<IEnumerable<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default, bool includeDeleted = false)
    {
        var stores = await _storeRepository.FindAsync(
            s => includeDeleted || !s.IsDeleted,
            cancellationToken);

        return stores.Select(store => new StoreDto(
            store.Id,
            store.Name,
            store.Code,
            store.Address,
            store.City,
            store.Phone,
            store.Email,
            store.IsActive,
            store.Version,
            store.IsDeleted));
    }

    public async Task<StoreDto> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var store = new Store
        {
            Id = request.Id ?? Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            City = request.City,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = true
        };

        await _storeRepository.AddAsync(store, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StoreDto(
            store.Id,
            store.Name,
            store.Code,
            store.Address,
            store.City,
            store.Phone,
            store.Email,
            store.IsActive,
            store.Version,
            store.IsDeleted);
    }

    public async Task<StoreDto> UpdateAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Store with ID {request.Id} not found.");

        store.Name = request.Name;
        store.Code = request.Code;
        store.Address = request.Address;
        store.City = request.City;
        store.Phone = request.Phone;
        store.Email = request.Email;
        store.IsActive = request.IsActive;

        _storeRepository.Update(store);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StoreDto(
            store.Id,
            store.Name,
            store.Code,
            store.Address,
            store.City,
            store.Phone,
            store.Email,
            store.IsActive,
            store.Version,
            store.IsDeleted);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
    {
        var store = await _storeRepository.GetByIdAsync(id, cancellationToken);
        if (store == null)
            return false;

        store.IsDeleted = true;
        _storeRepository.Update(store);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}