using Application.Whitelist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor")]
[Route("api/whitelist")]
public sealed class WhitelistController(IWhitelistService whitelistService) : ControllerBase
{
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetBySession(string sessionId, CancellationToken cancellationToken)
    {
        var entries = await whitelistService.GetBySessionAsync(sessionId, cancellationToken);
        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CreateWhitelistRequest request, CancellationToken cancellationToken)
    {
        var created = await whitelistService.AddAsync(request, cancellationToken);
        return Ok(created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var removed = await whitelistService.RemoveAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
