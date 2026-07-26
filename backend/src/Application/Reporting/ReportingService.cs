using Application.Abstractions.Persistence;

namespace Application.Reporting;

public sealed class ReportingService(
    IObservationRepository observationRepository,
    IAlertRepository alertRepository) : IReportingService
{
    public async Task<SessionReportDto> BuildSessionReportAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var observations = await observationRepository.GetBySessionAsync(sessionId, cancellationToken);
        var alerts = await alertRepository.GetBySessionAsync(sessionId, cancellationToken);

        return new SessionReportDto(
            SessionId: sessionId,
            ObservationCount: observations.Count,
            AlertCount: alerts.Count,
            DistinctDeviceCount: observations.Select(x => x.DeviceId).Distinct().Count(),
            GeneratedAt: DateTimeOffset.UtcNow);
    }
}
