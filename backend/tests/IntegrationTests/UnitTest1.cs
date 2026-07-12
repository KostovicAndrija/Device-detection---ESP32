using Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests;

public class UnitTest1
{
    [Fact]
    public void Login_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        var controller = new AuthController();

        var result = controller.Login(new AuthController.LoginRequest(string.Empty, string.Empty));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Login_ReturnsToken_WhenPayloadIsValid()
    {
        var controller = new AuthController();

        var result = controller.Login(new AuthController.LoginRequest("admin", "password"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<AuthController.LoginResponse>(okResult.Value);
    }
}
