namespace Application.Reporting;

public interface IReportingService
{
    Task<SessionReportDto> BuildSessionReportAsync(string sessionId, CancellationToken cancellationToken = default);
}
