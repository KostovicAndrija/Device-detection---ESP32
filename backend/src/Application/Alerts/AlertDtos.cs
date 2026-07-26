namespace Application.Alerts;

public sealed record AlertDto(
    Guid Id,
    Guid DeviceId,
    string? SessionId,
    int RiskScore,
    string Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcknowledgedAt);
