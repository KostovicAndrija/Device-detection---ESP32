using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Worker.Monitoring;

public sealed class WorkerAccessTokenProvider(IConfiguration configuration)
{
    public string Create()
    {
        var issuer = configuration["Jwt:Issuer"] ?? "device-detection-api";
        var audience = configuration["Jwt:Audience"] ?? "device-detection-clients";
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey must be configured for the worker.");
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(ClaimTypes.Name, "mqtt-worker"),
                new Claim(ClaimTypes.Role, "Worker")
            ],
            now,
            now.AddMinutes(10),
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
