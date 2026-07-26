namespace Application.Alerts;

public interface IAlertService
{
    Task<IReadOnlyList<AlertDto>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default);
}
