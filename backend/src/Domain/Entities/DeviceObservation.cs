namespace Domain.Entities;

public sealed record DeviceObservation(
    string DeviceHash,
    string SensorId,
    string? SessionId,
    double Rssi,
    DateTimeOffset CapturedAt);
