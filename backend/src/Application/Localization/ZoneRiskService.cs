using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Application.Localization;

public sealed class ZoneRiskService(
    IObservationRepository observations,
    IDeviceRepository devices,
    IWhitelistRepository whitelist,
    IPositionEstimator estimator,
    IRoomLayoutProvider layouts,
    IOptionsMonitor<DeskRiskOptions> configuredOptions) : IZoneRiskService
{
    public async Task<IReadOnlyList<DeskRiskDto>> GetBySessionAsync(string sessionId, int windowMinutes = 0,
        DateTimeOffset? at = null, CancellationToken cancellationToken = default, DeskRiskOptions? criteriaOverride = null)
    {
        var criteria = criteriaOverride ?? configuredOptions.CurrentValue;
        if (!criteria.IsValid()) throw new OptionsValidationException(DeskRiskOptions.SectionName,
            typeof(DeskRiskOptions), ["Neispravni kriterijumi kritične klupe u DeskRisk konfiguraciji."]);
        var bucketSeconds = criteria.BucketSeconds;
        var totalBuckets = windowMinutes > 0
            ? (int)Math.Ceiling(Math.Clamp(windowMinutes, 1, 30) * 60d / bucketSeconds)
            : criteria.WindowBucketCount;
        var until = at ?? DateTimeOffset.UtcNow;
        var from = until.AddSeconds(-totalBuckets * bucketSeconds);
        var room = await layouts.ForSessionAsync(Guid.Parse(sessionId), cancellationToken);
        var rows = await observations.GetWindowAsync(sessionId, from, until, cancellationToken);
        var sensorMap = room.Sensors.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var known = await devices.GetByIdsAsync(rows.Select(x => x.DeviceId).Distinct().ToArray(), cancellationToken);
        var hashes = known.ToDictionary(x => x.Id, x => x.HashId);
        var entries = await whitelist.GetBySessionAsync(sessionId, cancellationToken);
        var allowed = entries.Where(x => x.ValidFrom <= until && (x.ValidTo == null || x.ValidTo >= from))
            .Select(x => x.DeviceHash).ToHashSet(StringComparer.Ordinal);

        var hits = new List<ZoneHit>();
        foreach (var group in rows.Where(x => sensorMap.ContainsKey(x.SensorId) && x.CapturedAt > from && x.CapturedAt <= until)
                     .GroupBy(x => new { x.DeviceId, x.SignalType,
                         Bucket = (long)((until - x.CapturedAt).TotalSeconds / bucketSeconds) }))
        {
            if (hashes.TryGetValue(group.Key.DeviceId, out var hash) && allowed.Contains(hash)) continue;
            var latest = group.GroupBy(x => x.SensorId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.OrderByDescending(v => v.CapturedAt).First()).ToArray();
            if (latest.Length < criteria.MinimumSensorCount) continue;
            if ((latest.Max(x => x.CapturedAt) - latest.Min(x => x.CapturedAt)).TotalSeconds >
                criteria.MaximumSensorTimeSpreadSeconds) continue;
            var readings = latest.Select(x => {
                var sensor = sensorMap[x.SensorId];
                return new SensorReadingDto(sensor.Id, sensor.X, sensor.Y, x.Rssi, sensor.ReferenceRssi, sensor.PathLossExponent);
            }).ToArray();
            var estimate = await estimator.EstimateAsync(room, readings, cancellationToken);
            if (estimate is null || estimate.Confidence < Math.Clamp(criteria.MinimumPositionConfidence, 0, 1) ||
                !double.IsFinite(estimate.X) || !double.IsFinite(estimate.Y)) continue;
            var desk = NearestDesk(room, estimate.X, estimate.Y, Math.Max(0, criteria.DeskAssociationRadiusMeters));
            if (desk is null) continue;
            hits.Add(new ZoneHit(desk.Id, group.Key.DeviceId, group.Key.SignalType, group.Key.Bucket, estimate.Confidence,
                group.Min(x => x.CapturedAt), group.Max(x => x.CapturedAt)));
        }

        return room.Computers.Where(x => !x.IsTeacherDesk && (x.Kind == "desk" || x.ComputerCount > 0))
            .Select(desk => Build(desk, hits.Where(x => x.DeskId.Equals(desk.Id, StringComparison.OrdinalIgnoreCase)).ToArray(), totalBuckets, criteria))
            .OrderByDescending(x => x.Score).ThenBy(x => x.DeskId).ToArray();
    }

    private static DeskRiskDto Build(ComputerLocation desk, IReadOnlyCollection<ZoneHit> hits, int totalBuckets, DeskRiskOptions criteria)
    {
        if (hits.Count == 0) return new(desk.Id, 0, "normal", 0, 0, totalBuckets, 0, 0, null, null);
        var deviceCount = hits.Select(x => x.DeviceId).Distinct().Count();
        var result = DeskRiskCalculator.Calculate(hits.Select(x => (x.SignalType, x.Bucket)), totalBuckets, criteria);
        var confidence = hits.Average(x => x.Confidence);
        return new(desk.Id, result.Score, result.Level, deviceCount, result.ActiveBuckets, totalBuckets,
            Math.Round(result.Frequency, 2), Math.Round(confidence, 2), hits.Min(x => x.First), hits.Max(x => x.Last), result.SignalType);
    }

    private static ComputerLocation? NearestDesk(RoomLayout room, double x, double y, double associationRadius)
        => room.Computers.Where(d => !d.IsTeacherDesk && (d.Kind == "desk" || d.ComputerCount > 0))
            .Select(d => new { Desk = d, Distance = DistanceToRectangle(d, room, x, y) })
            .Where(x => x.Distance <= associationRadius).OrderBy(x => x.Distance).Select(x => x.Desk).FirstOrDefault();

    private static double DistanceToRectangle(ComputerLocation desk, RoomLayout room, double x, double y)
    {
        var width = desk.Width ?? room.Width * .13;
        var height = desk.Height ?? room.Height * .08;
        var dx = Math.Max(Math.Abs(x - desk.X) - width / 2, 0);
        var dy = Math.Max(Math.Abs(y - desk.Y) - height / 2, 0);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed record ZoneHit(string DeskId, Guid DeviceId, string SignalType, long Bucket, double Confidence,
        DateTimeOffset First, DateTimeOffset Last);
}
