using Application.Monitoring;

namespace Infrastructure.Monitoring;

public sealed class NoopMonitoringEventPublisher : IMonitoringEventPublisher
{
    public Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishAlertAcknowledgedAsync(AlertAcknowledgedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishSessionStateChangedAsync(SessionStateChangedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
