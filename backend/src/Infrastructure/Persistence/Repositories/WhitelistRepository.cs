using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class WhitelistRepository(AppDbContext dbContext) : IWhitelistRepository
{
    public async Task<IReadOnlyList<WhitelistEntry>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => await dbContext.WhitelistEntries
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.StudentRef)
            .ToListAsync(cancellationToken);

    public Task AddAsync(WhitelistEntry entry, CancellationToken cancellationToken = default)
        => dbContext.WhitelistEntries.AddAsync(entry, cancellationToken).AsTask();

    public Task<WhitelistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.WhitelistEntries.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> IsWhitelistedAsync(string sessionId, string deviceHash, CancellationToken cancellationToken = default)
        => dbContext.WhitelistEntries.AnyAsync(
            x => x.SessionId == sessionId && x.DeviceHash == deviceHash && (x.ValidTo == null || x.ValidTo >= DateTimeOffset.UtcNow),
            cancellationToken);

    public void Remove(WhitelistEntry entry)
        => dbContext.WhitelistEntries.Remove(entry);
}
