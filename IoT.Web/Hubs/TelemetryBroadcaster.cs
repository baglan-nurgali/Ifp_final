using IoT.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using IoT.Web.State;

namespace IoT.Web.Hubs;

public sealed class TelemetryBroadcaster(TelemetryPipeline pipeline, IHubContext<TelemetryHub,ITelemetryClient> hub, DashboardStateContainer state) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach(var batch in pipeline.Reader.ReadAllAsync(stoppingToken))
        { state.Apply(batch); await hub.Clients.Group("all").ReceiveBatch(batch); }
    }
}
