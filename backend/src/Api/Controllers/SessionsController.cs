using Application.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor")]
[Route("api/sessions")]
public sealed class SessionsController(IExamSessionService sessionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var sessions = await sessionService.GetAllAsync(cancellationToken);
        return Ok(sessions);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var created = await sessionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var session = await sessionService.StartAsync(id, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, CancellationToken cancellationToken)
    {
        var session = await sessionService.StopAsync(id, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }
}
