using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository(AppDbContext dbContext) : IObservationRepository
{
    public Task AddAsync(DeviceObservation observation, CancellationToken cancellationToken = default)
        => dbContext.DeviceObservations.AddAsync(observation, cancellationToken).AsTask();

    public async Task<IReadOnlyList<DeviceObservation>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => await dbContext.DeviceObservations
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CapturedAt)
            .ToListAsync(cancellationToken);
}
