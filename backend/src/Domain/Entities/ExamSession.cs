namespace Domain.Entities;

public sealed class ExamSession
{
    private ExamSession()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string RoomId { get; private set; } = string.Empty;
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public string Status { get; private set; } = "draft";
    public Guid? OwnerUserId { get; private set; }
    public DateTimeOffset? RegistrationExpiresAt { get; private set; }

    public void MarkAsRegistrationScan(DateTimeOffset expiresAt)
    {
        if (Status != "planned") throw new InvalidOperationException("Skeniranje mora prvo biti planirano.");
        RegistrationExpiresAt = expiresAt;
    }

    public void SetOwner(Guid userId)
    {
        if (Status != "planned" || OwnerUserId is not null || userId == Guid.Empty)
            throw new InvalidOperationException("Vlasnik se postavlja samo pri kreiranju sesije.");
        OwnerUserId = userId;
    }

    public static ExamSession Create(string name, string roomId, DateTimeOffset startsAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Session name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        return new ExamSession
        {
            Name = name.Trim(),
            RoomId = roomId.Trim(),
            StartsAt = startsAt,
            Status = "planned"
        };
    }

    public void Start(DateTimeOffset at)
    {
        if (Status != "planned")
        {
            throw new InvalidOperationException($"A session in '{Status}' status cannot be started.");
        }

        StartsAt = at;
        Status = "active";
    }

    public void Stop(DateTimeOffset at)
    {
        if (Status != "active")
        {
            throw new InvalidOperationException($"A session in '{Status}' status cannot be stopped.");
        }

        EndsAt = at;
        Status = "completed";
    }
}
