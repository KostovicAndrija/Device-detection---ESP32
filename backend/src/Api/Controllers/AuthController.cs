using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        return Ok(new LoginResponse($"demo-token-{Guid.NewGuid():N}", DateTimeOffset.UtcNow.AddHours(1)));
    }

    public sealed record LoginRequest(string Username, string Password);
    public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
}
