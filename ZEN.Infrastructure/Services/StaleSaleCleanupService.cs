using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZEN.Domain.Enums;
using ZEN.Infrastructure.Persistence;

namespace ZEN.Infrastructure.Services;

public class StaleSaleCleanupService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleSaleCleanupService> _logger;
    private Timer? _timer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(2);

    public StaleSaleCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleSaleCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StaleSaleCleanupService starting (interval: {Interval}s, stale threshold: {Threshold}min)",
            (int)_checkInterval.TotalSeconds, (int)_staleThreshold.TotalMinutes);
        _timer = new Timer(async _ => await DoCleanupAsync(), null, _checkInterval, _checkInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StaleSaleCleanupService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task DoCleanupAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ZenDbContext>();

            var staleThresholdTime = DateTime.UtcNow - _staleThreshold;
            var staleSales = await dbContext.Sales
                .Where(s => s.Status == SaleStatus.Waiting && s.Date < staleThresholdTime)
                .ToListAsync();

            if (!staleSales.Any())
            {
                return;
            }

            foreach (var sale in staleSales)
            {
                sale.Status = SaleStatus.Failed;
                sale.Notes = "Fiscalization timed out or was interrupted. Retry the sale.";
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Marked {Count} stale Waiting sales as Failed.", staleSales.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during stale sale cleanup.");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
