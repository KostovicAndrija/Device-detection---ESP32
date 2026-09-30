using Application.Abstractions.Security;
using Application.Localization;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController, Route("api/sessions/{id:guid}"), Authorize(Roles = "Professor,Assistant")]
public sealed class SessionHistoryController(AppDbContext db, ISessionAccess access, IRoomLayoutProvider layouts,
    IPositionQueryService positions, IPositionEstimator estimator) : ControllerBase
{
    [HttpGet("layout")]
    public async Task<IActionResult> Layout(Guid id, CancellationToken ct)
    {
        await access.EnsureAccessAsync(id, ct);
        var session = await db.ExamSessions.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();
        return Ok(new { layout = await layouts.ForSessionAsync(id, ct), hasSnapshot = session.RoomSnapshotJson != null });
    }
    [HttpGet("history")]
    public async Task<IActionResult> History(Guid id, CancellationToken ct)
    {
        await access.EnsureAccessAsync(id, ct);
        var session = await db.ExamSessions.SingleOrDefaultAsync(s => s.Id == id, ct);
        if (session is null) return NotFound();
        var rows = db.DeviceObservations.Where(o => o.SessionId == id.ToString());
        return Ok(new { observationCount = await rows.CountAsync(ct),
            firstAt = await rows.MinAsync(o => (DateTimeOffset?)o.CapturedAt, ct),
            lastAt = await rows.MaxAsync(o => (DateTimeOffset?)o.CapturedAt, ct),
            hasSnapshot = session.RoomSnapshotJson != null, estimatorVersion = estimator.Version,
            mode = "recomputed", status = session.Status });
    }
    [HttpGet("history/frame")]
    public async Task<IActionResult> Frame(Guid id, [FromQuery] DateTimeOffset at, CancellationToken ct)
    {
        await access.EnsureAccessAsync(id, ct);
        if (at == default || at > DateTimeOffset.UtcNow.AddMinutes(5)) throw new ArgumentException("Izaberite važeće vreme reprodukcije.");
        return Ok(await positions.AtAsync(id.ToString(), at, ct));
    }
    // Versioned, paged export for offline analysis/training. No raw MAC addresses or credentials.
    [HttpGet("history/observations")]
    public async Task<IActionResult> Observations(Guid id, [FromQuery] int page = 0, [FromQuery] int pageSize = 1000, CancellationToken ct = default)
    {
        await access.EnsureAccessAsync(id, ct);
        if (page < 0 || page > 1000000 || pageSize is < 1 or > 1000) throw new ArgumentException("Neispravna stranica izvoza.");
        if (!await db.ExamSessions.AnyAsync(s => s.Id == id, ct)) return NotFound();
        var rows = await db.DeviceObservations.AsNoTracking().Where(o => o.SessionId == id.ToString())
            .OrderBy(o => o.CapturedAt).ThenBy(o => o.Id).Skip(page * pageSize).Take(pageSize + 1)
            .Select(o => new { o.Id, o.DeviceId, o.SensorId, o.SignalType, o.Rssi, o.CapturedAt }).ToListAsync(ct);
        return Ok(new { schemaVersion = 1, sessionId = id, estimatorVersion = estimator.Version,
            page, hasMore = rows.Count > pageSize, observations = rows.Take(pageSize), layout = await layouts.ForSessionAsync(id, ct) });
    }
}
