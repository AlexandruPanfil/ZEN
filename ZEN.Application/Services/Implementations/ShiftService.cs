using ZEN.Application.DTOs.Shifts;
using ZEN.Application.Services.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class ShiftService : IShiftService
{
    public Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ShiftDto> CloseShiftAsync(CloseShiftRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ShiftDto?> GetCurrentShiftAsync(Guid storeId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<ShiftDto?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<ShiftDto>> GetShiftsByStoreAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<CashMovementDto> CreateCashMovementAsync(CreateCashMovementRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<IEnumerable<CashMovementDto>> GetCashMovementsByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
