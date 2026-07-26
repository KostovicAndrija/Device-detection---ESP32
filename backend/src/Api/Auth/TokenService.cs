using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

public sealed class TokenService(
    IOptions<JwtOptions> options,
    IOptions<DevelopmentUserOptions> userOptions)
{
    private readonly JwtOptions _options = options.Value;
    private readonly DevelopmentUserOptions _user = userOptions.Value;
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _refreshStore = new(StringComparer.Ordinal);

    public bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return FixedTimeEquals(username.Trim(), _user.Username) &&
               FixedTimeEquals(password, _user.Password);
    }

    public TokenPair Issue(string username)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Professor")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = $"rf-{Guid.NewGuid():N}";
        _refreshStore[refreshToken] = new RefreshTokenEntry(
            username,
            now.AddDays(Math.Max(1, _options.RefreshTokenDays)));

        return new TokenPair(accessToken, refreshToken, expires);
    }

    public TokenPair? Refresh(string refreshToken)
    {
        if (!_refreshStore.TryRemove(refreshToken, out var entry) ||
            entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return Issue(entry.Username);
    }

    public bool Revoke(string refreshToken)
        => _refreshStore.TryRemove(refreshToken, out _);

    private static bool FixedTimeEquals(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private sealed record RefreshTokenEntry(string Username, DateTimeOffset ExpiresAt);
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
