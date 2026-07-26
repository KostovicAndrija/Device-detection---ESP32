using Application.Abstractions.Persistence;

namespace Application.Alerts;

public sealed class AlertService(IAlertRepository repository, IAppUnitOfWork unitOfWork) : IAlertService
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

        alert.Acknowledge(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AlertDto Map(Domain.Entities.Alert alert)
        => new(alert.Id, alert.DeviceId, alert.SessionId, alert.RiskScore, alert.Reason, alert.CreatedAt, alert.AcknowledgedAt);
}
