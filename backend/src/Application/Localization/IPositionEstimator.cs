using Domain.Entities;
namespace Application.Localization;

// Replace this adapter with a local/remote AI model without changing sensors or UI.
public interface IPositionEstimator
{
    string Version { get; }
    Task<LocationEstimateDto?> EstimateAsync(RoomLayout room, IReadOnlyCollection<SensorReadingDto> readings, CancellationToken ct = default);
}
public sealed class RssiPositionEstimator(ILocalizationService localization) : IPositionEstimator
{
    public string Version => "rssi-wls-kalman-window60-v1";
    public Task<LocationEstimateDto?> EstimateAsync(RoomLayout room, IReadOnlyCollection<SensorReadingDto> readings, CancellationToken ct = default)
        => Task.FromResult(localization.Estimate(readings));
}
