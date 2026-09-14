using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface IStoreService
{
    Task<StoreDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoreDto>> GetAllAsync(CancellationToken cancellationToken = default, bool includeDeleted = false);
    Task<StoreDto> CreateAsync(CreateStoreRequest request, CancellationToken cancellationToken = default);
    Task<StoreDto> UpdateAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0);
}