using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ZEN.Windows.Services;

public class SignalRService : IDisposable, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRService> _logger;
    private readonly string _baseUrl;
    private readonly string _token;
    private Guid? _currentStoreId;
    private bool _isConnected = false;

    public event Action<bool>? ConnectionStatusChanged;
    public event Action<object>? CashDeskStatusChanged;
    public event Action<object>? HeldSaleCreated;
    public event Action<object>? HeldSaleCompleted;
    public event Action<object>? ShiftOpened;
    public event Action<object>? ShiftClosed;

    public SignalRService(string baseUrl, string token, ILogger<SignalRService> logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _token = token;
        _logger = logger;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hubs/cashdesk", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_token)!;
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    private void RegisterHandlers()
    {
        _hubConnection.On<object>("CashDeskStatusChanged", data =>
        {
            _logger.LogDebug("Received CashDeskStatusChanged: {Data}", data);
            CashDeskStatusChanged?.Invoke(data);
        });

        _hubConnection.On<object>("HeldSaleCreated", data =>
        {
            _logger.LogDebug("Received HeldSaleCreated: {Data}", data);
            HeldSaleCreated?.Invoke(data);
        });

        _hubConnection.On<object>("HeldSaleCompleted", data =>
        {
            _logger.LogDebug("Received HeldSaleCompleted: {Data}", data);
            HeldSaleCompleted?.Invoke(data);
        });

        _hubConnection.On<object>("ShiftOpened", data =>
        {
            _logger.LogDebug("Received ShiftOpened: {Data}", data);
            ShiftOpened?.Invoke(data);
        });

        _hubConnection.On<object>("ShiftClosed", data =>
        {
            _logger.LogDebug("Received ShiftClosed: {Data}", data);
            ShiftClosed?.Invoke(data);
        });

        _hubConnection.Closed += async (error) =>
        {
            _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
        };

        _hubConnection.Reconnecting += async (error) =>
        {
            _logger.LogInformation("SignalR reconnecting: {Error}", error?.Message);
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _logger.LogInformation("SignalR reconnected: {ConnectionId}", connectionId);
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(true);
            
            // Re-join store group
            if (_currentStoreId.HasValue)
            {
                await JoinStoreGroupAsync(_currentStoreId.Value);
            }
        };
    }

    public async Task StartAsync(Guid storeId, CancellationToken cancellationToken = default)
    {
        if (_isConnected && _currentStoreId == storeId)
            return;

        _currentStoreId = storeId;

        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync(cancellationToken);
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(true);
            }

            await JoinStoreGroupAsync(storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
            throw;
        }
    }

    private async Task JoinStoreGroupAsync(Guid storeId)
    {
        try
        {
            await _hubConnection.InvokeAsync("JoinStoreGroup", storeId);
            _logger.LogDebug("Joined store group: {StoreId}", storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join store group: {StoreId}", storeId);
        }
    }

    public async Task StopAsync()
    {
        if (_currentStoreId.HasValue)
        {
            try
            {
                await _hubConnection.InvokeAsync("LeaveStoreGroup", _currentStoreId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave store group");
            }
        }

        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }

        _isConnected = false;
        ConnectionStatusChanged?.Invoke(false);
    }

    public void Dispose()
    {
        _hubConnection.DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}