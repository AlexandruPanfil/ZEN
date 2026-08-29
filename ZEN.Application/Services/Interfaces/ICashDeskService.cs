using ZEN.Application.DTOs.Admin;
using ZEN.Application.DTOs.CashDeskMenu;

namespace ZEN.Application.Services.Interfaces;

public interface ICashDeskService
{
    Task<CashDeskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CashDeskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashDeskDto>> GetAllAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
    Task<CashDeskDto> CreateAsync(CreateCashDeskRequest request, CancellationToken cancellationToken = default);
    Task<CashDeskDto> UpdateAsync(UpdateCashDeskRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CashDeskDto> AssignMenuAsync(AssignMenuToCashDeskRequest request, CancellationToken cancellationToken = default);
    
    // New methods for POS integration
    Task<CashDeskDto?> HeartbeatAsync(Guid cashDeskId, CancellationToken cancellationToken = default);
    Task<IEnumerable<CashDeskDto>> GetAllWithStatusAsync(Guid? storeId = null, CancellationToken cancellationToken = default);
}

public record CreateCashDeskRequest(
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId,
    Guid? MenuId,
    IEnumerable<Guid> TerminalIds);

public record UpdateCashDeskRequest(
    Guid Id,
    string Name,
    Guid? StoreId,
    bool IsActive,
    Guid? FiscalPrinterId,
    Guid? CustomerDisplayId,
    Guid? MenuId,
    IEnumerable<Guid> TerminalIds);
