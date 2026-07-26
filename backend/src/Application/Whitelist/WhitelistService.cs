using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Domain.Entities;

namespace Application.Whitelist;

public sealed class WhitelistService(
    IWhitelistRepository repository,
    IDeviceHashingService hashingService,
    IAppUnitOfWork unitOfWork) : IWhitelistService
{
    public async Task<IReadOnlyList<WhitelistEntryDto>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetBySessionAsync(sessionId, cancellationToken);
        return entries.Select(Map).ToList();
    }

    public async Task<WhitelistEntryDto> AddAsync(CreateWhitelistRequest request, CancellationToken cancellationToken = default)
    {
        var hash = hashingService.Hash(request.DeviceIdentifier);
        var entry = WhitelistEntry.Create(request.SessionId, request.StudentRef, hash, request.ValidFrom, request.ValidTo);

        await repository.AddAsync(entry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entry);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        repository.Remove(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WhitelistEntryDto Map(WhitelistEntry entry)
        => new(entry.Id, entry.SessionId, entry.StudentRef, entry.DeviceHash, entry.ValidFrom, entry.ValidTo);
}
