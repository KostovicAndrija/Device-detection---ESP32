namespace Domain.Entities;

public sealed class Device
{
    private Device()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string HashId { get; private set; } = string.Empty;
    public string Type { get; private set; } = "unknown";
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    public static Device Create(string hashId, DateTimeOffset seenAt, string? type = null)
    {
        if (string.IsNullOrWhiteSpace(hashId))
        {
            throw new ArgumentException("Device hash is required.", nameof(hashId));
        }

        if (seenAt == default)
        {
            throw new ArgumentException("Seen timestamp is required.", nameof(seenAt));
        }

        return new Device
        {
            HashId = hashId.Trim(),
            Type = string.IsNullOrWhiteSpace(type) ? "unknown" : type.Trim().ToLowerInvariant(),
            FirstSeenAt = seenAt,
            LastSeenAt = seenAt
        };
    }

    public void Touch(DateTimeOffset seenAt)
    {
        if (seenAt > LastSeenAt)
        {
            LastSeenAt = seenAt;
        }
    }
}
