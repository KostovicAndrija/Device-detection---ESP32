namespace Application.Localization;

public sealed record DeskRiskDto(
    string DeskId,
    int Score,
    string Level,
    int DeviceCount,
    int ActiveBuckets,
    int TotalBuckets,
    double NormalizedFrequency,
    double AverageConfidence,
    DateTimeOffset? FirstSeenAt,
    DateTimeOffset? LastSeenAt,
    string? SignalType = null);

public interface IZoneRiskService
{
    Task<IReadOnlyList<DeskRiskDto>> GetBySessionAsync(string sessionId, int windowMinutes = 0,
        DateTimeOffset? at = null, CancellationToken cancellationToken = default, DeskRiskOptions? criteriaOverride = null);
}
