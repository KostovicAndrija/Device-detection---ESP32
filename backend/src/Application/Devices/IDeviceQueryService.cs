namespace Application.Devices;

public interface IDeviceQueryService
{
    Task<IReadOnlyList<DeviceDto>> GetActiveAsync(TimeSpan window, CancellationToken cancellationToken = default);
}
