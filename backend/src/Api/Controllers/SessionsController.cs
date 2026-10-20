using Application.Alerts;
using Application.Localization;
using Application.Sessions;
using Application.Whitelist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/sessions")]
public sealed class SessionsController(
    IExamSessionService sessionService,
    IAlertService alertService,
    IWhitelistService whitelistService,
    IPositionQueryService positionQueryService,
    IZoneRiskService zoneRiskService,
    AppDbContext db) : ControllerBase
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
            if (await SensorReadinessError(request.RoomId, cancellationToken) is { } readinessError) return readinessError;
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
            var existing = await sessionService.GetByIdAsync(id, cancellationToken);
            if (existing is null) return NotFound();
            if (await SensorReadinessError(existing.RoomId, cancellationToken) is { } readinessError) return readinessError;
            var session = await sessionService.StartAsync(id, cancellationToken);
            return Ok(session);
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
            var result = await sessionService.StopAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = StatusCodes.Status409Conflict });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Professor")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await sessionService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
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

    [HttpGet("{id:guid}/zones")]
    public async Task<IActionResult> GetZones(Guid id, [FromQuery] int windowMinutes = 0, CancellationToken cancellationToken = default)
        => Ok(await zoneRiskService.GetBySessionAsync(id.ToString(), windowMinutes, cancellationToken: cancellationToken));

    [HttpPost("{id:guid}/zones/preview")]
    public async Task<IActionResult> PreviewZones(Guid id, [FromBody] Application.Localization.DeskRiskOptions criteria,
        [FromQuery] DateTimeOffset? at = null, CancellationToken cancellationToken = default)
    {
        if (!criteria.IsValid()) return BadRequest(new ProblemDetails { Title = "Neispravna podešavanja detekcije. Proverite trajanje intervala i pragove." });
        return Ok(await zoneRiskService.GetBySessionAsync(id.ToString(), at: at,
            cancellationToken: cancellationToken, criteriaOverride: criteria));
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

    private async Task<IActionResult?> SensorReadinessError(string roomId, CancellationToken cancellationToken)
    {
        var classroom = await db.Classrooms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == roomId, cancellationToken);
        if (classroom is null) return NotFound();
        var expected = classroom.Layout().Sensors.Select(x => x.Id).ToArray();
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-20);
        var online = await db.SensorNodes.AsNoTracking()
            .Where(x => x.ClassroomId == roomId && x.LastHeartbeatAt >= cutoff)
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var onlineSet = online.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = expected.Where(x => !onlineSet.Contains(x)).ToArray();
        if (expected.Length >= 3 && onlineSet.Count >= 3 && missing.Length == 0) return null;
        var detail = expected.Length < 3
            ? "Učionica nema konfigurisana najmanje tri senzora."
            : $"Nisu aktivni svi senzori. Nedostaju: {string.Join(", ", missing)}.";
        return Conflict(new ProblemDetails
        {
            Title = "Praćenje nije pokrenuto jer senzori nisu spremni.",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        });
    }
}
