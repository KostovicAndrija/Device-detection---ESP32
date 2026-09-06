using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

public sealed class TokenService(
    AppDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Username == normalized, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<TokenPair> IssueAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            now.UtcDateTime,
            expires.UtcDateTime,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var rawRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        await dbContext.RefreshTokens.AddAsync(
            RefreshToken.Create(user.Id, HashToken(rawRefreshToken), now.AddDays(Math.Max(1, _options.RefreshTokenDays))),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new TokenPair(new JwtSecurityTokenHandler().WriteToken(token), rawRefreshToken, expires);
    }

    public async Task<TokenPair?> RefreshAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(rawToken);
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null || token.RevokedAt is not null || token.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var user = await dbContext.Users.FindAsync([token.UserId], cancellationToken);
        if (user is null)
        {
            return null;
        }

        token.Revoke(DateTimeOffset.UtcNow);
        return await IssueAsync(user, cancellationToken);
    }

    public async Task<bool> RevokeAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(rawToken);
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null)
        {
            return false;
        }

        token.Revoke(DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
