namespace Domain.Entities;

public sealed class DeviceObservation
{
    private DeviceObservation()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DeviceId { get; private set; }
    public string SensorId { get; private set; } = string.Empty;
    public string? SessionId { get; private set; }
    public string SignalType { get; private set; } = "wifi";
    public double Rssi { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public string? ExternalId { get; private set; }

    public static DeviceObservation Create(
        Guid deviceId,
        string sensorId,
        string? sessionId,
        string signalType,
        double rssi,
        DateTimeOffset capturedAt,
        string? externalId = null)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("Device id is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(sensorId))
        {
            throw new ArgumentException("Sensor id is required.", nameof(sensorId));
        }

        if (rssi is < -120 or > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rssi), "RSSI must be between -120 and 0 dBm.");
        }

        if (capturedAt == default)
        {
            throw new ArgumentException("Capture timestamp is required.", nameof(capturedAt));
        }

        return new DeviceObservation
        {
            DeviceId = deviceId,
            SensorId = sensorId.Trim(),
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim(),
            SignalType = string.IsNullOrWhiteSpace(signalType) ? "wifi" : signalType.Trim().ToLowerInvariant(),
            Rssi = rssi,
            CapturedAt = capturedAt,
            ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId.Trim()
        };
    }
}
