using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IObservationRepository
{
    Task AddAsync(DeviceObservation observation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceObservation>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
