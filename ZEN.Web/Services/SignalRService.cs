using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ZEN.Web.Services;

public class SignalRService : IDisposable, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<SignalRService> _logger;
    private readonly string _baseUrl;
    private Guid? _currentStoreId;
    private bool _isConnected = false;

    public event Action<bool>? ConnectionStatusChanged;
    public event Action<object>? CashDeskStatusChanged;
    public event Action<object>? HeldSaleCreated;
    public event Action<object>? HeldSaleCompleted;
    public event Action<object>? ShiftOpened;
    public event Action<object>? ShiftClosed;

    public SignalRService(string baseUrl, ILogger<SignalRService> logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _logger = logger;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hubs/cashdesk")
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
        {
            _logger.LogInformation("SignalR already connected to store {StoreId}", storeId);
            return;
        }

        _currentStoreId = storeId;
        _logger.LogInformation("Starting SignalR connection to {BaseUrl} for store {StoreId}", _baseUrl, storeId);

        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                _logger.LogInformation("SignalR hub is disconnected, starting connection...");
                await _hubConnection.StartAsync(cancellationToken);
                _isConnected = true;
                ConnectionStatusChanged?.Invoke(true);
                _logger.LogInformation("SignalR connection started successfully");
            }
            else
            {
                _logger.LogInformation("SignalR hub state: {State}", _hubConnection.State);
            }

            _logger.LogInformation("Joining store group {StoreId}", storeId);
            await JoinStoreGroupAsync(storeId);
            _logger.LogInformation("Successfully joined store group {StoreId}", storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection to {BaseUrl} for store {StoreId}: {Message}", _baseUrl, storeId, ex.Message);
            _isConnected = false;
            ConnectionStatusChanged?.Invoke(false);
            throw;
        }
    }

    private async Task JoinStoreGroupAsync(Guid storeId)
    {
        try
        {
            _logger.LogDebug("Invoking JoinStoreGroup for store {StoreId}", storeId);
            await _hubConnection.InvokeAsync("JoinStoreGroup", storeId);
            _logger.LogInformation("Successfully joined store group: {StoreId}", storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join store group: {StoreId}. Error: {Message}", storeId, ex.Message);
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