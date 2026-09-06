namespace Domain.Entities;

public sealed class WhitelistEntry
{
    private WhitelistEntry()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SessionId { get; private set; } = string.Empty;
    public string StudentRef { get; private set; } = string.Empty;
    public string DeviceHash { get; private set; } = string.Empty;
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }

    public static WhitelistEntry Create(
        string sessionId,
        string studentRef,
        string deviceHash,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session id is required.", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(studentRef))
        {
            throw new ArgumentException("Student reference is required.", nameof(studentRef));
        }

        if (string.IsNullOrWhiteSpace(deviceHash))
        {
            throw new ArgumentException("Device hash is required.", nameof(deviceHash));
        }

        if (validTo.HasValue && validTo <= validFrom)
        {
            throw new ArgumentException("Valid-to must be later than valid-from.", nameof(validTo));
        }

        return new WhitelistEntry
        {
            SessionId = sessionId.Trim(),
            StudentRef = studentRef.Trim(),
            DeviceHash = deviceHash.Trim(),
            ValidFrom = validFrom,
            ValidTo = validTo
        };
    }
}
