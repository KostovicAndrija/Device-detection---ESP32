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
