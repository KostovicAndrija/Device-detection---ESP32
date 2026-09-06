namespace Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Actor { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Create(string actor, string action, string resource, int statusCode)
        => new()
        {
            Actor = actor[..Math.Min(actor.Length, 128)],
            Action = action[..Math.Min(action.Length, 16)],
            Resource = resource[..Math.Min(resource.Length, 512)],
            StatusCode = statusCode,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
