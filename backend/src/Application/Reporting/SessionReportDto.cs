namespace Application.Reporting;

public sealed record SessionReportDto(
    string SessionId,
    int ObservationCount,
    int AlertCount,
    int DistinctDeviceCount,
    DateTimeOffset GeneratedAt);
