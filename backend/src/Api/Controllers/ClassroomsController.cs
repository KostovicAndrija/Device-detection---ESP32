using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController, Route("api/classrooms"), Authorize(Roles = "Professor,Assistant")]
public sealed class ClassroomsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok((await db.Classrooms.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct)).Select(r => r.Layout()));

    [HttpGet("{id}/sensor-status")]
    public async Task<IActionResult> SensorStatus(string id, CancellationToken ct)
    {
        var classroom = await db.Classrooms.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (classroom is null) return NotFound();
        var now = DateTimeOffset.UtcNow;
        var nodes = await db.SensorNodes.AsNoTracking().Where(x => x.ClassroomId == id).ToListAsync(ct);
        var byId = nodes.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        return Ok(classroom.Layout().Sensors.Select(sensor =>
        {
            byId.TryGetValue(sensor.Id, out var node);
            var age = node is null ? double.PositiveInfinity : (now - node.LastHeartbeatAt).TotalSeconds;
            var state = age <= 20 ? "online" : age <= 45 ? "stale" : "offline";
            return new
            {
                sensor.Id, State = state, node?.LastHeartbeatAt, node?.FirmwareVersion,
                IsSimulated = node?.IsSimulated ?? false
            };
        }));
    }
    [HttpPost, Authorize(Roles = "Professor")]
    public async Task<IActionResult> Create(RoomLayout layout, CancellationToken ct)
    {
        layout = layout.WithDefaultBoard();
        var room = Classroom.Create(layout);
        if (await db.Classrooms.AnyAsync(r => r.Id == layout.Id, ct)) throw new InvalidOperationException("Oznaka učionice već postoji.");
        db.Classrooms.Add(room); await db.SaveChangesAsync(ct); return Ok(room.Layout());
    }
    [HttpPut("{id}"), Authorize(Roles = "Professor")]
    public async Task<IActionResult> Update(string id, RoomLayout layout, CancellationToken ct)
    {
        var room = await db.Classrooms.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (room is null) return NotFound();
        layout = layout.WithDefaultBoard();
        room.Update(layout); await db.SaveChangesAsync(ct); return Ok(room.Layout());
    }
}
