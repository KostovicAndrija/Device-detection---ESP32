namespace Domain.Entities;

public sealed class AppUser
{
    private AppUser() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Professor";

    public static AppUser Create(string username, string role = "Professor")
        => new()
        {
            Username = username.Trim().ToLowerInvariant(),
            Role = role.Trim()
        };

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
