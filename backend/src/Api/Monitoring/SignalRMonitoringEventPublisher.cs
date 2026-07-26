using Api.Hubs;
using Application.Monitoring;
using Microsoft.AspNetCore.SignalR;

namespace Api.Monitoring;

public sealed class SignalRMonitoringEventPublisher(IHubContext<MonitoringHub> hubContext) : IMonitoringEventPublisher
{
    public Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync("deviceUpdated", payload, cancellationToken);

    public Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync("alertCreated", payload, cancellationToken);
}
