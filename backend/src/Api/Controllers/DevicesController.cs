using Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor")]
[Route("api/devices")]
public sealed class DevicesController(IDeviceQueryService queryService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int windowMinutes = 10, CancellationToken cancellationToken = default)
    {
        var devices = await queryService.GetActiveAsync(TimeSpan.FromMinutes(Math.Max(1, windowMinutes)), cancellationToken);
        return Ok(devices);
    }
}
