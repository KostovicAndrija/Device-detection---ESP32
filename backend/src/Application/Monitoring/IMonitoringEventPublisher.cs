namespace Application.Monitoring;

public interface IMonitoringEventPublisher
{
    Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default);
    Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default);
    Task PublishAlertAcknowledgedAsync(AlertAcknowledgedEvent payload, CancellationToken cancellationToken = default);
    Task PublishSessionStateChangedAsync(SessionStateChangedEvent payload, CancellationToken cancellationToken = default);
}
