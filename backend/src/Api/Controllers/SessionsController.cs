using Application.Alerts;
using Application.Localization;
using Application.Sessions;
using Application.Whitelist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/sessions")]
public sealed class SessionsController(
    IExamSessionService sessionService,
    IAlertService alertService,
    IWhitelistService whitelistService,
    IPositionQueryService positionQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var sessions = await sessionService.GetAllAsync(cancellationToken);
        return Ok(sessions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await sessionService.GetByIdAsync(id, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var created = await sessionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
    }

    [HttpPost("start-for-room")]
    public async Task<IActionResult> StartForRoom(
        [FromBody] StartRoomSessionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.StartForRoomAsync(request, cancellationToken);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.StartAsync(id, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<IActionResult> Stop(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var session = await sessionService.StopAsync(id, cancellationToken);
            return session is null ? NotFound() : Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpGet("{id:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid id, CancellationToken cancellationToken)
    {
        var alerts = await alertService.GetBySessionAsync(id.ToString(), cancellationToken);
        return Ok(alerts);
    }

    [HttpGet("{id:guid}/positions")]
    public async Task<IActionResult> GetPositions(Guid id, CancellationToken cancellationToken)
    {
        var positions = await positionQueryService.GetBySessionAsync(id.ToString(), cancellationToken);
        return Ok(positions);
    }

    [HttpGet("{id:guid}/whitelist")]
    public async Task<IActionResult> GetWhitelist(Guid id, CancellationToken cancellationToken)
    {
        var entries = await whitelistService.GetBySessionAsync(id.ToString(), cancellationToken);
        return Ok(entries);
    }

    [HttpPost("{id:guid}/whitelist")]
    public async Task<IActionResult> AddWhitelist(
        Guid id,
        [FromBody] SessionWhitelistRequest request,
        CancellationToken cancellationToken)
    {
        var created = await whitelistService.AddAsync(
            new CreateWhitelistRequest(id.ToString(), request.StudentRef, request.DeviceIdentifier, request.ValidFrom, request.ValidTo),
            cancellationToken);
        return Ok(created);
    }

    [HttpDelete("{id:guid}/whitelist/{entryId:guid}")]
    public async Task<IActionResult> RemoveWhitelist(Guid id, Guid entryId, CancellationToken cancellationToken)
    {
        var removed = await whitelistService.RemoveAsync(entryId, id.ToString(), cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    public sealed record SessionWhitelistRequest(
        string StudentRef,
        string DeviceIdentifier,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidTo);
}
