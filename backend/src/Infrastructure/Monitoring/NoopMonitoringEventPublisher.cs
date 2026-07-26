using Application.Monitoring;

namespace Infrastructure.Monitoring;

public sealed class NoopMonitoringEventPublisher : IMonitoringEventPublisher
{
    public Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
