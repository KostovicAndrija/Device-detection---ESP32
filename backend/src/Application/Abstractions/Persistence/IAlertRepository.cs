using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IAlertRepository
{
    Task AddAsync(Alert alert, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Alert>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
