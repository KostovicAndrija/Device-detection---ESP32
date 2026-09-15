using Application.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/alerts")]
public sealed class AlertsController(IAlertService alertService) : ControllerBase
{
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetBySession(string sessionId, CancellationToken cancellationToken)
    {
        var alerts = await alertService.GetBySessionAsync(sessionId, cancellationToken);
        return Ok(alerts);
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var acknowledged = await alertService.AcknowledgeAsync(id, cancellationToken);
        return acknowledged ? NoContent() : NotFound();
    }
}
