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
    private readonly IRepository<CashDesk> _cashDeskRepository;
    private readonly IStoreContext _storeContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public ShiftService(
        IRepository<Shift> shiftRepository,
        IRepository<CashMovement> cashMovementRepository,
        IRepository<CashDesk> cashDeskRepository,
        IStoreContext storeContext,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _cashMovementRepository = cashMovementRepository;
        _cashDeskRepository = cashDeskRepository;
        _storeContext = storeContext;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftDto> OpenShiftAsync(OpenShiftRequest request, CancellationToken cancellationToken = default)
    {
        var storeIdFromContext = _storeContext.GetCurrentStoreId();
        Guid storeId;
        if (storeIdFromContext.HasValue)
        {
            storeId = storeIdFromContext.Value;
        }
        else if (request.StoreId != Guid.Empty)
        {
            storeId = request.StoreId;
        }
        else
        {
            throw new InvalidOperationException("No store context available and StoreId not provided in request");
        }

        var existing = await _shiftRepository.FindFirstAsync(s => s.StoreId == storeId && s.Status == ShiftStatus.Open, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A shift is already open for this store");

        var shift = new Shift
        {
            UserId = _currentUserService.UserId ?? "system",
            StoreId = storeId,
            OpenTime = DateTime.UtcNow,
            OpeningCash = request.OpeningCash,
            ClosingCash = 0,
            Status = ShiftStatus.Open,
            Notes = null,
            CashDeskId = null
        };

        await _shiftRepository.AddAsync(shift, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(shift);
    }

    public async Task<ShiftDto> OpenShiftForCashDeskAsync(Guid cashDeskId, decimal openingCash, CancellationToken cancellationToken = default)
    {
        var cashDesk = await _cashDeskRepository.GetByIdAsync(cashDeskId, cancellationToken);
        if (cashDesk == null)
            throw new KeyNotFoundException($"Cash desk {cashDeskId} not found");

        if (!cashDesk.StoreId.HasValue)
            throw new InvalidOperationException($"Cash desk {cashDeskId} is not assigned to a store");

        var storeId = cashDesk.StoreId.Value;

        var existing = await _shiftRepository.FindFirstAsync(s => s.StoreId == storeId && s.Status == ShiftStatus.Open, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("A shift is already open for this store");

        var shift = new Shift
        {
            UserId = _currentUserService.UserId ?? "system",
            StoreId = storeId,
            OpenTime = DateTime.UtcNow,
            OpeningCash = openingCash,
            ClosingCash = 0,
            Status = ShiftStatus.Open,
            Notes = null,
            CashDeskId = cashDeskId
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

private static ShiftDto MapToDto(Shift shift)
        {
            var successfulSales = shift.Sales?.Where(s => s.Status != SaleStatus.Failed) ?? Enumerable.Empty<Sale>();
            var totalSales = successfulSales.Sum(s => s.TotalAmount);
            var salesCount = successfulSales.Count();
            var expectedCash = shift.OpeningCash + totalSales;
            var variance = shift.Status == ShiftStatus.Closed ? shift.ClosingCash - expectedCash : (decimal?)null;

            return new ShiftDto(
                shift.Id,
                $"SHIFT-{shift.Id.ToString("N")[..6].ToUpperInvariant()}",
                shift.OpenTime,
                shift.CloseTime,
                shift.UserId,
                "Unknown", // UserName - will be populated from CurrentUserService in context
                shift.StoreId,
                "Unknown", // StoreName
                shift.OpeningCash,
                shift.Status == ShiftStatus.Closed ? shift.ClosingCash : null,
                expectedCash,
                variance,
                shift.Status,
                totalSales,
                salesCount);
        }
}