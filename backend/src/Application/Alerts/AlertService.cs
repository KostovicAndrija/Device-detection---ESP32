using Application.Abstractions.Persistence;
using Application.Monitoring;

namespace Application.Alerts;

public sealed class AlertService(
    IAlertRepository repository,
    IAppUnitOfWork unitOfWork,
    IMonitoringEventPublisher monitoringEventPublisher) : IAlertService
{
    public async Task<IReadOnlyList<AlertDto>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var alerts = await repository.GetBySessionAsync(sessionId, cancellationToken);
        return alerts.Select(Map).ToList();
    }

    public async Task<bool> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var alert = await repository.GetByIdAsync(id, cancellationToken);
        if (alert is null)
        {
            return false;
        }

        var acknowledgedAt = DateTimeOffset.UtcNow;
        alert.Acknowledge(acknowledgedAt);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await monitoringEventPublisher.PublishAlertAcknowledgedAsync(
            new AlertAcknowledgedEvent(alert.Id, acknowledgedAt),
            cancellationToken);
        return true;
    }

    private static AlertDto Map(Domain.Entities.Alert alert)
        => new(alert.Id, alert.DeviceId, alert.SessionId, alert.RiskScore, alert.Reason, alert.CreatedAt, alert.AcknowledgedAt);
}
