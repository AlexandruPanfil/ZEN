using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ZEN.ApiService.Hubs;

public class CashDeskHub : Hub
{
    public async Task JoinStoreGroup(Guid storeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"store_{storeId}");
    }

    public async Task LeaveStoreGroup(Guid storeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"store_{storeId}");
    }
}