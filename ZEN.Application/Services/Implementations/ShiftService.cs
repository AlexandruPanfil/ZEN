using ZEN.Application.DTOs.Shifts;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Enums;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class ShiftService : IShiftService
{
    private readonly IRepository<Shift> _shiftRepository;
    private readonly IRepository<CashMovement> _cashMovementRepository;
    private readonly IStoreContext _storeContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ShiftService(
        IRepository<Shift> shiftRepository,
        IRepository<CashMovement> cashMovementRepository,
        IStoreContext storeContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _cashMovementRepository = cashMovementRepository;
        _storeContext = storeContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, CancellationToken cancellationToken = default)
    {
        var currentStoreId = _storeContext.GetCurrentStoreId() ?? request.StoreId;
        var existing = await _shiftRepository.FindFirstAsync(s => s.StoreId == currentStoreId && s.Status == ShiftStatus.Open, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A shift is already open for this store");

        var shift = new Shift
        {
            UserId = _currentUserService.UserId ?? "system",
            StoreId = currentStoreId,
            OpenTime = DateTime.UtcNow,
            OpeningCash = request.OpeningCash,
            ClosingCash = 0,
            Status = ShiftStatus.Open,
            Notes = null
        };

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(shift);
    }

    public async Task<ShiftDto> CloseShiftAsync(CloseShiftRequest request, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift == null)
            throw new KeyNotFoundException("Shift not found");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException("Only open shifts can be closed");

        shift.CloseTime = DateTime.UtcNow;
        shift.ClosingCash = request.ClosingCash;
        shift.Status = ShiftStatus.Closed;

        _shiftRepository.Update(shift);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(shift);
    }

    public async Task<ShiftDto?> GetCurrentShiftAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.FindFirstAsync(s => s.StoreId == storeId && s.Status == ShiftStatus.Open, cancellationToken);
        return shift == null ? null : MapToDto(shift);
    }

    public async Task<ShiftDto?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        return shift == null ? null : MapToDto(shift);
    }

    public async Task<IEnumerable<ShiftDto>> GetShiftsByStoreAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var shifts = await _shiftRepository.FindAsync(s => s.StoreId == storeId, cancellationToken);
        if (startDate.HasValue)
            shifts = shifts.Where(s => s.OpenTime >= startDate.Value).ToList();
        if (endDate.HasValue)
            shifts = shifts.Where(s => s.OpenTime <= endDate.Value).ToList();

        return shifts.OrderByDescending(s => s.OpenTime).Select(MapToDto);
    }

    public async Task<CashMovementDto> CreateCashMovementAsync(CreateCashMovementRequest request, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift == null)
            throw new KeyNotFoundException("Shift not found");

        var movement = new CashMovement
        {
            ShiftId = request.ShiftId,
            Type = request.Type,
            Amount = request.Amount,
            Notes = request.Reason,
            UserId = _currentUserService.UserId ?? "system",
            Timestamp = DateTime.UtcNow
        };

        await _cashMovementRepository.AddAsync(movement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CashMovementDto(movement.Id, movement.Timestamp, movement.Type, movement.Amount, movement.Notes ?? string.Empty, movement.UserId, _currentUserService.UserName ?? movement.UserId);
    }

    public async Task<IEnumerable<CashMovementDto>> GetCashMovementsByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var movements = await _cashMovementRepository.FindAsync(m => m.ShiftId == shiftId, cancellationToken);
        return movements.OrderBy(m => m.Timestamp).Select(m => new CashMovementDto(m.Id, m.Timestamp, m.Type, m.Amount, m.Notes ?? string.Empty, m.UserId, _currentUserService.UserName ?? m.UserId));
    }

    private ShiftDto MapToDto(Shift shift)
    {
        var totalSales = shift.Sales?.Sum(s => s.TotalAmount) ?? 0;
        var salesCount = shift.Sales?.Count ?? 0;
        var expectedCash = shift.OpeningCash + totalSales;
        var variance = shift.Status == ShiftStatus.Closed ? shift.ClosingCash - expectedCash : (decimal?)null;

        return new ShiftDto(
            shift.Id,
            $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
            shift.OpenTime,
            shift.CloseTime,
            shift.UserId,
            _currentUserService.UserName ?? shift.UserId,
            shift.StoreId,
            _storeContext.GetCurrentStoreNameAsync().GetAwaiter().GetResult() ?? "Unknown",
            shift.OpeningCash,
            shift.Status == ShiftStatus.Closed ? shift.ClosingCash : null,
            expectedCash,
            variance,
            shift.Status,
            totalSales,
            salesCount);
    }
}
