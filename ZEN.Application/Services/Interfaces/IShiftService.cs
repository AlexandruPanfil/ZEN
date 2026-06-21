using ZEN.Application.DTOs.Shifts;

namespace ZEN.Application.Services.Interfaces;

public interface IShiftService
{
    Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, CancellationToken cancellationToken = default);
    Task<ShiftDto> CloseShiftAsync(CloseShiftRequest request, CancellationToken cancellationToken = default);
    Task<ShiftDto?> GetCurrentShiftAsync(Guid storeId, CancellationToken cancellationToken = default);
    Task<ShiftDto?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ShiftDto>> GetShiftsByStoreAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<CashMovementDto> CreateCashMovementAsync(CreateCashMovementRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashMovementDto>> GetCashMovementsByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
}
