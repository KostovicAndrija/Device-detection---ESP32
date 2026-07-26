namespace Application.Monitoring;

public sealed record DeviceUpdatedEvent(
    string DeviceHash,
    string SensorId,
    string? SessionId,
    double Rssi,
    DateTimeOffset CapturedAt,
    int RiskScore);

public sealed record AlertCreatedEvent(
    Guid AlertId,
    string DeviceHash,
    string? SessionId,
    int RiskScore,
    string Reason,
    DateTimeOffset CreatedAt);
