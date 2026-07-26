using Application.Abstractions.Persistence;

namespace Application.Devices;

public sealed class DeviceQueryService(IDeviceRepository repository) : IDeviceQueryService
{
    public async Task<IReadOnlyList<DeviceDto>> GetActiveAsync(TimeSpan window, CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow.Subtract(window);
        var devices = await repository.GetActiveAsync(since, cancellationToken);
        return devices
            .Select(x => new DeviceDto(x.Id, x.HashId, x.Type, x.FirstSeenAt, x.LastSeenAt))
            .ToList();
    }
}
