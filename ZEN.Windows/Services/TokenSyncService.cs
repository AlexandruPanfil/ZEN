using ZEN.Windows.Services.Sync;

namespace ZEN.Windows.Services;

public sealed class TokenSyncService
{
    private readonly PosAuthState _authState;
    private readonly SyncApiClient _apiClient;

    public TokenSyncService(PosAuthState authState, SyncApiClient apiClient)
    {
        _authState = authState;
        _apiClient = apiClient;
        _authState.Changed += OnAuthStateChanged;
    }

    private void OnAuthStateChanged()
    {
        _apiClient.SetBearerToken(_authState.Token);
    }
}