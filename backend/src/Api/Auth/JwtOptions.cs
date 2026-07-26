namespace Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "device-detection-api";
    public string Audience { get; set; } = "device-detection-clients";
    public string SigningKey { get; set; } = "replace-this-with-very-long-signing-key";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class DevelopmentUserOptions
{
    public const string SectionName = "DevelopmentUser";

    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
}
