namespace Application.Monitoring;

public interface IMonitoringEventPublisher
{
    Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default);
    Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default);
}
