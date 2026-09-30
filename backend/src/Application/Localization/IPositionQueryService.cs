namespace Application.Localization;

public interface IPositionQueryService
{
    Task<IReadOnlyList<DevicePositionDto>> AtAsync(string sessionId, DateTimeOffset at, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevicePositionDto>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
