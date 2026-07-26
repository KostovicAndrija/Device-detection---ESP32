using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IWhitelistRepository
{
    Task<IReadOnlyList<WhitelistEntry>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task AddAsync(WhitelistEntry entry, CancellationToken cancellationToken = default);
    Task<WhitelistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsWhitelistedAsync(string sessionId, string deviceHash, CancellationToken cancellationToken = default);
    void Remove(WhitelistEntry entry);
}
