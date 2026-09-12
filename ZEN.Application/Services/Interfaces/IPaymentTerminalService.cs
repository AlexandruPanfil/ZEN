using ZEN.Application.DTOs.Admin;

namespace ZEN.Application.Services.Interfaces
{
    public interface IPaymentTerminalService
    {
        Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<PaymentTerminalDto>> GetAllAsync(
            CancellationToken cancellationToken = default,
            bool includeDeleted = false);
        IEnumerable<PaymentTerminalDto> GetAll();
        Task<PaymentTerminalDto> CreateAsync(CreatePaymentTerminalRequest request, CancellationToken cancellationToken = default);
        Task<PaymentTerminalDto> UpdateAsync(UpdatePaymentTerminalRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default, long baseVersion = 0);
    }

    public record CreatePaymentTerminalRequest(
        string Name,
        string TerminalType,
        string? SerialNumber,
        string? IpAddress,
        int Port,
        bool IsActive,
        Guid? Id = null);

    public record UpdatePaymentTerminalRequest(
        Guid Id,
        string Name,
        string TerminalType,
        string? SerialNumber,
        string? IpAddress,
        int Port,
        bool IsActive,
        long BaseVersion = 0);
}