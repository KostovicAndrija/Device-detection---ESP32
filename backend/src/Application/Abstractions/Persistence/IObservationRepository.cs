using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IObservationRepository
{
    Task<IReadOnlyList<DeviceObservation>> GetWindowAsync(string sessionId, DateTimeOffset from, DateTimeOffset until, CancellationToken ct = default);
    Task AddAsync(DeviceObservation observation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceObservation>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
}
