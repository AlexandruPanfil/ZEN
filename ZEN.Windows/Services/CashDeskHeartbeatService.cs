using Microsoft.Extensions.Logging;
using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public class CashDeskHeartbeatService : IDisposable
{
    private readonly SyncApiClient _apiClient;
    private readonly ILogger<CashDeskHeartbeatService> _logger;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private bool _isRunning = false;
    private Guid? _currentCashDeskId;

    public event Action<bool>? ConnectionStatusChanged;

    public CashDeskHeartbeatService(SyncApiClient apiClient, ILogger<CashDeskHeartbeatService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
    }

    public void Start(Guid cashDeskId)
    {
        if (_isRunning && _currentCashDeskId == cashDeskId)
            return;

        _currentCashDeskId = cashDeskId;
        _isRunning = true;
        _ = RunHeartbeatLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _isRunning = false;
        _currentCashDeskId = null;
        _cts.Cancel();
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_currentCashDeskId.HasValue)
                {
                    var result = await _apiClient.SendHeartbeatAsync(_currentCashDeskId.Value, cancellationToken);
                    var wasOnline = _currentCashDeskId.HasValue; // Track previous state
                    var isOnline = result != null;
                    
                    if (wasOnline != isOnline)
                    {
                        ConnectionStatusChanged?.Invoke(isOnline);
                    }
                    
                    if (isOnline)
                    {
                        _logger.LogDebug("Heartbeat sent for cash desk {CashDeskId}", _currentCashDeskId.Value);
                    }
                    else
                    {
                        _logger.LogWarning("Heartbeat failed for cash desk {CashDeskId}", _currentCashDeskId.Value);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat for cash desk {CashDeskId}", _currentCashDeskId);
                ConnectionStatusChanged?.Invoke(false);
            }

            try
            {
                await _timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
        _cts.Dispose();
    }
}