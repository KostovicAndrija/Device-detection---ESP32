using Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController(TokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!tokenService.ValidateCredentials(request.Username, request.Password))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var tokens = tokenService.Issue(request.Username.Trim());
        return Ok(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt));
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        var tokens = tokenService.Refresh(request.RefreshToken);
        return tokens is null
            ? Unauthorized()
            : Ok(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt));
    }

    [HttpPost("revoke")]
    public IActionResult Revoke([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        return tokenService.Revoke(request.RefreshToken) ? NoContent() : NotFound();
    }

    public sealed record LoginRequest(string Username, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
