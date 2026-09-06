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
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Username and password are required.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var user = await tokenService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var tokens = await tokenService.IssueAsync(user, cancellationToken);
        return Ok(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        var tokens = await tokenService.RefreshAsync(request.RefreshToken, cancellationToken);
        return tokens is null
            ? Unauthorized()
            : Ok(new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresAt));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token is required.");
        }

        return await tokenService.RevokeAsync(request.RefreshToken, cancellationToken) ? NoContent() : NotFound();
    }

    public sealed record LoginRequest(string Username, string Password);
    public sealed record RefreshRequest(string RefreshToken);
    public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
