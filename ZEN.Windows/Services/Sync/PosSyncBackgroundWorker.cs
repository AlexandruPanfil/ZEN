using Microsoft.Extensions.Logging;

namespace ZEN.Windows.Services.Sync;

public sealed class PosSyncBackgroundWorker
{
    private readonly IPosSyncService _syncService;
    private readonly ILogger<PosSyncBackgroundWorker> _logger;
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public PosSyncBackgroundWorker(IPosSyncService syncService, ILogger<PosSyncBackgroundWorker> logger)
    {
        _syncService = syncService;
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
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await _syncService.RunSyncAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
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