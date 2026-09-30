using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ObservationRepository(AppDbContext dbContext) : IObservationRepository
{
    public async Task<IReadOnlyList<DeviceObservation>> GetWindowAsync(string sessionId, DateTimeOffset from, DateTimeOffset until, CancellationToken ct = default)
        => await dbContext.DeviceObservations.AsNoTracking().Where(o => o.SessionId == sessionId && o.CapturedAt >= from && o.CapturedAt <= until)
            .OrderBy(o => o.CapturedAt).ThenBy(o => o.Id).ToListAsync(ct);
    public Task AddAsync(DeviceObservation observation, CancellationToken cancellationToken = default)
        => dbContext.DeviceObservations.AddAsync(observation, cancellationToken).AsTask();

    public async Task<IReadOnlyList<DeviceObservation>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => await dbContext.DeviceObservations
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CapturedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
        => dbContext.DeviceObservations.AnyAsync(x => x.ExternalId == externalId, cancellationToken);
}
