using ZEN.Application.DTOs.FiscalCloud;
using ZEN.Domain.Entities;

namespace ZEN.Application.Services.Interfaces;

public interface IFiscalCloudService
{
    Task<FiscalCloudSaleResponseDto> SendSaleAsync(
        Printer printer,
        FiscalCloudSaleRequestDto request,
        CancellationToken cancellationToken = default);

    Task<FiscalCloudSaleResponseDto> SendReturnAsync(
        Printer printer,
        FiscalCloudReturnRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> SendCloseDayAsync(
        Printer printer,
        FiscalCloudCloseDayRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> SendIntermediateTotalsAsync(
        Printer printer,
        FiscalCloudIntermediateTotalsRequestDto request,
        CancellationToken cancellationToken = default);

    Task<FiscalCloudStatusResponseDto> GetStatusAsync(
        Printer printer,
        CancellationToken cancellationToken = default);
}
