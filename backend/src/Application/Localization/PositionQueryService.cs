using Application.Abstractions.Persistence;
using Microsoft.Extensions.Options;

namespace Application.Localization;

public sealed class PositionQueryService(
    IObservationRepository observationRepository,
    ILocalizationService localizationService,
    IPositionSmoother positionSmoother,
    IOptions<LocalizationOptions> options) : IPositionQueryService
{
    private readonly LocalizationOptions _options = options.Value;

    public async Task<IReadOnlyList<DevicePositionDto>> GetBySessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var observations = await observationRepository.GetBySessionAsync(sessionId, cancellationToken);
        if (observations.Count == 0)
        {
            return [];
        }

        var sensorMap = _options.Sensors.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var cutoff = observations.Max(x => x.CapturedAt).AddSeconds(-Math.Max(1, _options.WindowSeconds));

        return observations
            .Where(x => x.CapturedAt >= cutoff && sensorMap.ContainsKey(x.SensorId))
            .GroupBy(x => x.DeviceId)
            .Select(group =>
            {
                var latest = group
                    .GroupBy(x => x.SensorId, StringComparer.OrdinalIgnoreCase)
                    .Select(sensorGroup => sensorGroup.MaxBy(x => x.CapturedAt)!)
                    .ToList();
                var readings = latest.Select(x =>
                {
                    var sensor = sensorMap[x.SensorId];
                    return new SensorReadingDto(sensor.Id, sensor.X, sensor.Y, x.Rssi);
                }).ToList();
                var estimate = localizationService.Estimate(readings);
                if (estimate is not null)
                {
                    estimate = positionSmoother.Smooth(group.Key, estimate);
                }
                return estimate is null ? null : new DevicePositionDto(
                    group.Key,
                    estimate.X,
                    estimate.Y,
                    estimate.Confidence,
                    latest.Max(x => x.CapturedAt),
                    readings.Count);
            })
            .Where(x => x is not null)
            .Cast<DevicePositionDto>()
            .ToList();
    }
}
