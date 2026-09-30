using System.Text.Json;
using Application.Localization;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class RoomLayoutProvider(AppDbContext db) : IRoomLayoutProvider
{
    public async Task CaptureAsync(ExamSession session, CancellationToken ct = default)
    {
        var room = await db.Classrooms.SingleOrDefaultAsync(r => r.Id == session.RoomId, ct)
            ?? throw new ArgumentException("Učionica nije registrovana. Profesor treba prvo da je doda.");
        session.CaptureRoom(room.LayoutJson);
    }
    public async Task<RoomLayout> ForSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.ExamSessions.SingleOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new ArgumentException("Sesija ne postoji.");
        if (session.RoomSnapshotJson is not null) return JsonSerializer.Deserialize<RoomLayout>(session.RoomSnapshotJson)!;
        var room = await db.Classrooms.SingleOrDefaultAsync(r => r.Id == session.RoomId, ct);
        return room?.Layout() ?? (Classroom.Defaults().First().Layout() with { Id = session.RoomId, Name = session.RoomId });
    }
}
