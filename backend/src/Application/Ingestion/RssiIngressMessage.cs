namespace Application.Ingestion;

public sealed record RssiIngressMessage(
    string DeviceIdentifier,
    string SensorId,
    string? SessionId,
    string SignalType,
    double Rssi,
    DateTimeOffset CreatedAt,
    string? EventId = null);
