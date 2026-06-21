using ZEN.Domain.Enums;

namespace ZEN.Application.DTOs.Shifts;

public record ShiftDto(
    Guid Id,
    string Number,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string UserId,
    string UserName,
    Guid StoreId,
    string StoreName,
    decimal OpeningCash,
    decimal? ClosingCash,
    decimal ExpectedCash,
    decimal? Variance,
    ShiftStatus Status,
    decimal TotalSales,
    int SalesCount);

public record OpenShiftRequest(
    Guid StoreId,
    decimal OpeningCash);

public record CloseShiftRequest(
    Guid ShiftId,
    decimal ClosingCash);

public record CashMovementDto(
    Guid Id,
    DateTime Date,
    CashMovementType Type,
    decimal Amount,
    string Reason,
    string UserId,
    string UserName);

public record CreateCashMovementRequest(
    Guid ShiftId,
    CashMovementType Type,
    decimal Amount,
    string Reason);
