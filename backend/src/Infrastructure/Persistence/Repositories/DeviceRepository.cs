using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository(AppDbContext dbContext) : IDeviceRepository
{
    public Task<Device?> GetByHashAsync(string hashId, CancellationToken cancellationToken = default)
        => dbContext.Devices.SingleOrDefaultAsync(x => x.HashId == hashId, cancellationToken);

    public Task AddAsync(Device device, CancellationToken cancellationToken = default)
        => dbContext.Devices.AddAsync(device, cancellationToken).AsTask();

    public async Task<IReadOnlyList<Device>> GetActiveAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
        => await dbContext.Devices
            .Where(x => x.LastSeenAt >= since)
            .OrderByDescending(x => x.LastSeenAt)
            .ToListAsync(cancellationToken);
}
