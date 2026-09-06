using Api.Auth;
using Api.Controllers;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IntegrationTests;

public class UnitTest1
{
    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        await using var db = CreateDb();
        var controller = new AuthController(CreateTokenService(db));

        var result = await controller.Login(
            new AuthController.LoginRequest(string.Empty, string.Empty),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenPayloadIsValid()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<AppUser>();
        var user = AppUser.Create("admin");
        user.SetPasswordHash(hasher.HashPassword(user, "password"));
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var controller = new AuthController(CreateTokenService(db, hasher));

        var result = await controller.Login(
            new AuthController.LoginRequest("admin", "password"),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AuthController.AuthResponse>(okResult.Value);
        Assert.Single(db.RefreshTokens);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static TokenService CreateTokenService(
        AppDbContext db,
        IPasswordHasher<AppUser>? hasher = null)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "test-signing-key-with-enough-length-123456789"
        });
        return new TokenService(db, hasher ?? new PasswordHasher<AppUser>(), options);
    }
}
