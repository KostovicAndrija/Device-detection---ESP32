using Api.Controllers;
using Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IntegrationTests;

public class UnitTest1
{
    [Fact]
    public void Login_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        var controller = new AuthController(CreateTokenService());

        var result = controller.Login(new AuthController.LoginRequest(string.Empty, string.Empty));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_ReturnsToken_WhenPayloadIsValid()
    {
        var controller = new AuthController(CreateTokenService());

        var result = controller.Login(new AuthController.LoginRequest("admin", "password"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AuthController.AuthResponse>(okResult.Value);
    }

    private static TokenService CreateTokenService()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "test-signing-key-with-enough-length-123456789"
        });

        return new TokenService(options);
    }
}
