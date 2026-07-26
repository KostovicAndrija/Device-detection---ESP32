namespace Application.Whitelist;

public interface IWhitelistService
{
    Task<IReadOnlyList<WhitelistEntryDto>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<WhitelistEntryDto> AddAsync(CreateWhitelistRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
