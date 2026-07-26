namespace Domain.Entities;

public sealed class Alert
{
    private Alert()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DeviceId { get; private set; }
    public string? SessionId { get; private set; }
    public int RiskScore { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public static Alert Create(Guid deviceId, string? sessionId, int riskScore, string reason, DateTimeOffset createdAt)
    {
        if (deviceId == Guid.Empty)
        {
            throw new ArgumentException("Device id is required.", nameof(deviceId));
        }

        if (riskScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(riskScore), "Risk score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Alert reason is required.", nameof(reason));
        }

        return new Alert
        {
            DeviceId = deviceId,
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? null : sessionId.Trim(),
            RiskScore = riskScore,
            Reason = reason.Trim(),
            CreatedAt = createdAt
        };
    }

    public void Acknowledge(DateTimeOffset at)
    {
        AcknowledgedAt = at;
    }
}
