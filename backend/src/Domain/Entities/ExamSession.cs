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
        StartsAt = at;
        Status = "active";
    }

    public void Stop(DateTimeOffset at)
    {
        EndsAt = at;
        Status = "completed";
    }
}
