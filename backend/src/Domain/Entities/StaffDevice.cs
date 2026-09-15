namespace Domain.Entities;

public sealed class StaffDevice
{
    private StaffDevice() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string DeviceHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid RegistrationSessionId { get; private set; }

    public static StaffDevice Create(Guid userId, string label, string hash, Guid registrationSessionId)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(label) || label.Trim().Length > 100 || string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Unesite naziv i identifikator uređaja.");
        if (registrationSessionId == Guid.Empty) throw new ArgumentException("Registraciono skeniranje je obavezno.");
        return new StaffDevice { UserId = userId, Label = label.Trim(), DeviceHash = hash, RegistrationSessionId = registrationSessionId, CreatedAt = DateTimeOffset.UtcNow };
    }
}
