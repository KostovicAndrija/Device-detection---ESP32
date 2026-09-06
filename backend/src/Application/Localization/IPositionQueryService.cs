namespace Application.Localization;

public interface IPositionQueryService
{
    Task<IReadOnlyList<DevicePositionDto>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
