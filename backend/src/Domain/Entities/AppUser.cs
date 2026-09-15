namespace Domain.Entities;

public sealed class AppUser
{
    private AppUser() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Professor";
    public int StaffDeviceRevision { get; private set; }

    public void TouchStaffDevices() => StaffDeviceRevision++;

    public static AppUser Create(string username, string role = "Professor")
        => role is not ("Professor" or "Assistant")
            ? throw new ArgumentException("Nepoznata korisnička uloga.", nameof(role))
            : new()
        {
            Username = username.Trim().ToLowerInvariant(),
            Role = role.Trim()
        };

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
