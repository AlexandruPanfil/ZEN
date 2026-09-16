using ZEN.Application.DTOs.Admin;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class StoreService : IStoreService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StoreService(IRepository<Store> storeRepository, IRepository<Currency> currencyRepository, IUnitOfWork unitOfWork)
    {
        _storeRepository = storeRepository;
        _currencyRepository = currencyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(id, [s => s.Currency], cancellationToken);
        if (store == null)
            return null;

        return ToDto(store);
    }

    public async Task<IEnumerable<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default, bool includeDeleted = false)
    {
        var stores = await _storeRepository.FindAsync(
            s => includeDeleted || !s.IsDeleted,
            [s => s.Currency],
            cancellationToken);

        return stores.Select(ToDto);
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

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var currency = await _currencyRepository.FindFirstAsync(c => c.Code == request.CurrencyCode, cancellationToken);
            store.CurrencyId = currency?.Id;
        }

        await _storeRepository.AddAsync(store, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(store);
    }

    public async Task<StoreDto> UpdateAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default)
    {
        var store = await _storeRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Store with ID {request.Id} not found.");

        if (store.IsDeleted || (request.BaseVersion != 0 && request.BaseVersion != store.Version))
        {
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                store.Id,
                request.BaseVersion,
                store.Version);
        }

        store.Name = request.Name;
        store.Code = request.Code;
        store.Address = request.Address;
        store.City = request.City;
        store.Phone = request.Phone;
        store.Email = request.Email;
        store.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            var currency = await _currencyRepository.FindFirstAsync(c => c.Code == request.CurrencyCode, cancellationToken);
            store.CurrencyId = currency?.Id;
        }

        _storeRepository.Update(store);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(store);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0)
    {
        var store = await _storeRepository.GetByIdAsync(id, cancellationToken);
        if (store == null)
            return false;

        if (store.IsDeleted || (baseVersion != 0 && baseVersion != store.Version))
        {
            throw new ZEN.Application.Services.Exceptions.SyncConflictException(
                store.Id,
                baseVersion,
                store.Version);
        }

        store.IsDeleted = true;
        _storeRepository.Update(store);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static StoreDto ToDto(Store store) => new(
        store.Id,
        store.Name,
        store.Code,
        store.Address,
        store.City,
        store.Phone,
        store.Email,
        store.IsActive,
        store.Version,
        store.IsDeleted,
        store.Currency?.Code);
}