using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces;

public interface ICashDeskService
{
    Task<CashDeskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashDeskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashDeskDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<CashDeskDto> CreateAsync(CreateCashDeskRequest request, CancellationToken cancellationToken = default);
    Task<CashDeskDto> UpdateAsync(UpdateCashDeskRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public record CreateCashDeskRequest(
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId,
    IEnumerable<Guid> TerminalIds);

public record UpdateCashDeskRequest(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId,
    IEnumerable<Guid> TerminalIds);