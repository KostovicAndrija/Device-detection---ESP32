using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task<Device?> GetByHashAsync(string hashId, CancellationToken cancellationToken = default);
    Task AddAsync(Device device, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Device>> GetActiveAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
}
