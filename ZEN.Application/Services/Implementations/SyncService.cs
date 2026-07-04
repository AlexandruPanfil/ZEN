using System.Text.Json;
using ZEN.Application.DTOs.Sync;
using ZEN.Application.Services.Interfaces;
using ZEN.Domain.Entities;
using ZEN.Domain.Interfaces;

namespace ZEN.Application.Services.Implementations;

public class SyncService : ISyncService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<SalePayment> _salePaymentRepository;
    private readonly IRepository<ProcessedSyncMessage> _processedSyncMessageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SyncService(
        IRepository<Product> productRepository,
        IRepository<SalePayment> salePaymentRepository,
        IRepository<ProcessedSyncMessage> processedSyncMessageRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _salePaymentRepository = salePaymentRepository;
        _processedSyncMessageRepository = processedSyncMessageRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductsDeltaResponse> GetProductsDeltaAsync(DateTime? sinceUtc, int pageSize = 500, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);

        var products = sinceUtc.HasValue
            ? await _productRepository.FindAsync(p => p.UpdatedAt > sinceUtc.Value, cancellationToken)
            : await _productRepository.GetAllAsync(cancellationToken);

        var ordered = products
            .OrderBy(p => p.UpdatedAt)
            .Take(pageSize + 1)
            .ToList();

        var hasMore = ordered.Count > pageSize;
        var page = hasMore ? ordered.Take(pageSize).ToList() : ordered;

        var items = page
            .Select(p => new ProductSyncItemDto(
                p.Id,
                p.Name,
                p.Barcode,
                p.SKU,
                p.CostPrice,
                p.SalePrice,
                p.IsActive,
                p.IsDeleted,
                p.UpdatedAt))
            .ToList();

        return new ProductsDeltaResponse(DateTime.UtcNow, hasMore, items);
    }

    public async Task<UploadPaymentsResponse> UploadPaymentsAsync(UploadPaymentsRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            return new UploadPaymentsResponse([]);
        }

        var results = new List<PaymentSyncResultDto>(request.Items.Count);

        foreach (var item in request.Items)
        {
            try
            {
                var alreadyProcessed = await _processedSyncMessageRepository.ExistsAsync(
                    m => m.IdempotencyKey == item.IdempotencyKey,
                    cancellationToken);

                if (alreadyProcessed)
                {
                    results.Add(new PaymentSyncResultDto(item.PaymentId, "Duplicate", "Already processed"));
                    continue;
                }

                var existingPayment = await _salePaymentRepository.ExistsAsync(p => p.Id == item.PaymentId, cancellationToken);
                if (!existingPayment)
                {
                    var payment = new SalePayment
                    {
                        Id = item.PaymentId,
                        SaleId = item.SaleId,
                        Amount = item.Amount,
                        Method = item.Method,
                        Reference = item.Reference,
                        PaidAt = item.PaidAt,
                        ProcessedAt = item.ProcessedAt
                    };

                    await _salePaymentRepository.AddAsync(payment, cancellationToken);
                }

                await _processedSyncMessageRepository.AddAsync(new ProcessedSyncMessage
                {
                    IdempotencyKey = item.IdempotencyKey,
                    Source = request.PosDeviceId,
                    ProcessedAt = DateTime.UtcNow
                }, cancellationToken);

                results.Add(new PaymentSyncResultDto(item.PaymentId, "Accepted", null));
            }
            catch (Exception ex)
            {
                results.Add(new PaymentSyncResultDto(item.PaymentId, "Rejected", ex.Message));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UploadPaymentsResponse(results);
    }
}