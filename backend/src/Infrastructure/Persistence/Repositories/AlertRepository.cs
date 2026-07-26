using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class AlertRepository(AppDbContext dbContext) : IAlertRepository
{
    public Task AddAsync(Alert alert, CancellationToken cancellationToken = default)
        => dbContext.Alerts.AddAsync(alert, cancellationToken).AsTask();

    public async Task<IReadOnlyList<Alert>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => await dbContext.Alerts
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Alerts.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
