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

    public Task PublishAlertAcknowledgedAsync(AlertAcknowledgedEvent payload, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync("alertAcknowledged", payload, cancellationToken);

    public Task PublishSessionStateChangedAsync(SessionStateChangedEvent payload, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync("sessionStateChanged", payload, cancellationToken);
}
