using IoT.Application;
using Microsoft.AspNetCore.SignalR;

namespace IoT.Web.Hubs;

public interface ITelemetryClient
{
    Task ReceiveBatch(TelemetryBatch batch);
    Task ConnectionStatus(string status);
}
public sealed class TelemetryHub : Hub<ITelemetryClient>
{
    public async Task Subscribe(string group = "all") => await Groups.AddToGroupAsync(Context.ConnectionId,group);
    public async Task Unsubscribe(string group = "all") => await Groups.RemoveFromGroupAsync(Context.ConnectionId,group);
    public override async Task OnConnectedAsync() { await Clients.Caller.ConnectionStatus("Connected"); await base.OnConnectedAsync(); }
}
