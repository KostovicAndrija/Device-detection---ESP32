namespace Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt)
        => new() { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAt };

    public void Revoke(DateTimeOffset at) => RevokedAt ??= at;
}
