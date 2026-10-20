namespace Domain.Entities;

public sealed class SensorNode
{
    private SensorNode() { }

    public string Id { get; private set; } = string.Empty;
    public string ClassroomId { get; private set; } = string.Empty;
    public DateTimeOffset LastHeartbeatAt { get; private set; }
    public string FirmwareVersion { get; private set; } = "unknown";
    public bool IsSimulated { get; private set; }

    public static SensorNode Register(string id, string classroomId, DateTimeOffset heartbeatAt,
        string? firmwareVersion, bool isSimulated)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(classroomId))
            throw new ArgumentException("Sensor id and classroom id are required.");

        return new SensorNode
        {
            Id = id.Trim(),
            ClassroomId = classroomId.Trim(),
            LastHeartbeatAt = heartbeatAt,
            FirmwareVersion = string.IsNullOrWhiteSpace(firmwareVersion) ? "unknown" : firmwareVersion.Trim(),
            IsSimulated = isSimulated
        };
    }

    public void Heartbeat(DateTimeOffset at, string? firmwareVersion, bool isSimulated)
    {
        if (at > LastHeartbeatAt) LastHeartbeatAt = at;
        if (!string.IsNullOrWhiteSpace(firmwareVersion)) FirmwareVersion = firmwareVersion.Trim();
        IsSimulated = isSimulated;
    }
}
