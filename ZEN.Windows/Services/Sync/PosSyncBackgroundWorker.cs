using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ZEN.Windows.Services.Sync;

public sealed class PosSyncBackgroundWorker
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PosSyncBackgroundWorker> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private int _syncRequested;

    public PosSyncBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PosSyncBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Start()
    {
        if (_runTask is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _runTask = Task.Run(() => RunAsync(_cts.Token), _cts.Token);
        _logger.LogInformation("POS background sync worker started; first run scheduled in 10 seconds, then every 10 seconds.");
    }

    public void RequestSync()
    {
        Interlocked.Exchange(ref _syncRequested, 1);
        _logger.LogDebug("POS synchronization requested; the next scheduled run will process the outbox.");
    }

    public async Task StopAsync()
    {
        if (_cts is null || _runTask is null)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            await _runTask;
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _runTask = null;
        }
        }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IPosSyncService>();
                    await syncService.RunSyncAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "POS synchronization iteration failed; the worker will retry.");
                }

                if (Interlocked.Exchange(ref _syncRequested, 0) == 1)
                {
                    continue;
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POS background sync worker crashed.");
        }
    }

}
