namespace Application.Ingestion;

public sealed record RssiIngressMessage(
    string DeviceHash,
    string SensorId,
    string? SessionId,
    double Rssi,
    DateTimeOffset CreatedAt);
