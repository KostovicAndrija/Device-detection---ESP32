using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Application.Localization;

public sealed class PositionQueryService(IObservationRepository observations, IDeviceRepository devices,
    IWhitelistRepository whitelist, IPositionEstimator estimator, IRoomLayoutProvider layouts,
    IOptions<LocalizationOptions> options) : IPositionQueryService
{
    public Task<IReadOnlyList<DevicePositionDto>> GetBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => AtAsync(sessionId, DateTimeOffset.UtcNow, cancellationToken);

    public async Task<IReadOnlyList<DevicePositionDto>> AtAsync(string sessionId, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var room = await layouts.ForSessionAsync(Guid.Parse(sessionId), cancellationToken);
        var rows = await observations.GetWindowAsync(sessionId, at.AddSeconds(-60), at, cancellationToken);
        var sensorMap = room.Sensors.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        var entries = await whitelist.GetBySessionAsync(sessionId, cancellationToken);
        var allowed = entries.Where(e => e.ValidFrom <= at && (e.ValidTo == null || e.ValidTo >= at)).Select(e => e.DeviceHash).ToHashSet();
        var known = await devices.GetByIdsAsync(rows.Select(o => o.DeviceId).Distinct().ToArray(), cancellationToken);
        var hashes = known.ToDictionary(d => d.Id, d => d.HashId);
        var result = new List<DevicePositionDto>();
        // Fixed warm-up and request-local state isolate sessions and make seeking deterministic.
        var smoother = new KalmanPositionSmoother();
        foreach (var group in rows.Where(o => sensorMap.ContainsKey(o.SensorId)).GroupBy(o => o.DeviceId))
        {
            var latest = new Dictionary<string, DeviceObservation>(StringComparer.OrdinalIgnoreCase);
            LocationEstimateDto? estimate = null;
            List<DeviceObservation> sample = [];
            foreach (var instant in group.OrderBy(o => o.CapturedAt).ThenBy(o => o.Id).GroupBy(o => o.CapturedAt))
            {
                foreach (var row in instant) latest[row.SensorId] = row;
                sample = latest.Values.Where(o => o.CapturedAt >= instant.Key.AddSeconds(-Math.Clamp(options.Value.WindowSeconds, 1, 60))).ToList();
                var readings = sample.Select(o => {
                    var sensor = sensorMap[o.SensorId];
                    return new SensorReadingDto(sensor.Id, sensor.X, sensor.Y, o.Rssi, sensor.ReferenceRssi, sensor.PathLossExponent);
                }).ToList();
                var raw = await estimator.EstimateAsync(room, readings, cancellationToken);
                if (raw is not null && double.IsFinite(raw.X) && double.IsFinite(raw.Y) && double.IsFinite(raw.Confidence))
                    estimate = smoother.Smooth(group.Key, raw with { X = Math.Clamp(raw.X, 0, room.Width), Y = Math.Clamp(raw.Y, 0, room.Height), Confidence = Math.Clamp(raw.Confidence, 0, 1) });
            }
            var captured = group.Max(o => o.CapturedAt);
            if (estimate is null || captured < at.AddSeconds(-45)) continue;
            var types = sample.Select(o => NormalizeSignal(o.SignalType)).Distinct().ToArray();
            result.Add(new(group.Key, estimate.X, estimate.Y, estimate.Confidence, captured, sample.Count,
                hashes.TryGetValue(group.Key, out var hash) && allowed.Contains(hash), types.Length > 1 ? "mixed" : types[0]));
        }
        return result;
    }
    public static string NormalizeSignal(string value) => value.Contains("bluetooth", StringComparison.OrdinalIgnoreCase) || value.Contains("ble", StringComparison.OrdinalIgnoreCase)
        ? "bluetooth" : value.Contains("wifi", StringComparison.OrdinalIgnoreCase) || value.Contains("wi-fi", StringComparison.OrdinalIgnoreCase) ? "wifi" : "unknown";
}
