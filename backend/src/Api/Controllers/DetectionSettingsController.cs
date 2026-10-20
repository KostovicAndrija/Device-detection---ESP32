using Application.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/detection-settings")]
public sealed class DetectionSettingsController(IOptionsMonitor<DeskRiskOptions> options) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(options.CurrentValue);
}
