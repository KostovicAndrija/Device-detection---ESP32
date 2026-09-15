using Api.Hubs;
using Application.Monitoring;
using Microsoft.AspNetCore.SignalR;
using Infrastructure.Persistence;

namespace Api.Monitoring;

public sealed class SignalRMonitoringEventPublisher(IHubContext<MonitoringHub> hubContext, AppDbContext db) : IMonitoringEventPublisher
{
    private async Task Send(string name, object payload, CancellationToken ct)
        => await hubContext.Clients.Groups(await MonitoringAudience.Groups(db, payload, ct)).SendAsync(name, payload, ct);
    public Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default)
        => Send("deviceUpdated", payload, cancellationToken);

    public Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default)
        => Send("alertCreated", payload, cancellationToken);

    public Task PublishAlertAcknowledgedAsync(AlertAcknowledgedEvent payload, CancellationToken cancellationToken = default)
        => Send("alertAcknowledged", payload, cancellationToken);

    public Task PublishSessionStateChangedAsync(SessionStateChangedEvent payload, CancellationToken cancellationToken = default)
        => Send("sessionStateChanged", payload, cancellationToken);
}
