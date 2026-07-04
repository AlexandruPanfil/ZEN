namespace ZEN.Windows.Services.Sync;

public interface IPosSyncService
{
    Task RunSyncAsync(CancellationToken cancellationToken = default);
}