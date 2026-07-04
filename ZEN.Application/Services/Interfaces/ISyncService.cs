using ZEN.Application.DTOs.Sync;

namespace ZEN.Application.Services.Interfaces;

public interface ISyncService
{
    Task<ProductsDeltaResponse> GetProductsDeltaAsync(DateTime? sinceUtc, int pageSize = 500, CancellationToken cancellationToken = default);
    Task<UploadPaymentsResponse> UploadPaymentsAsync(UploadPaymentsRequest request, CancellationToken cancellationToken = default);
}