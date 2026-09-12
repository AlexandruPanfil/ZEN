using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class TokenSyncService
{
    private readonly PosAuthState _authState;
    private readonly SyncApiClient _apiClient;
    private readonly PosSyncBackgroundWorker _backgroundWorker;

    public TokenSyncService(
        PosAuthState authState,
        SyncApiClient apiClient,
        PosSyncBackgroundWorker backgroundWorker)
    {
        _authState = authState;
        _apiClient = apiClient;
        _backgroundWorker = backgroundWorker;
        _authState.Changed += OnAuthStateChanged;
        _apiClient.SetBearerToken(_authState.Token);
    }

    private void OnAuthStateChanged()
    {
        _apiClient.SetBearerToken(_authState.Token);

        if (!string.IsNullOrWhiteSpace(_authState.Token))
        {
            _backgroundWorker.RequestSync();
        }
    }
}