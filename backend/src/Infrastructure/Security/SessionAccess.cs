using Application.Abstractions.Security;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid? Id => null;
    public bool IsProfessor => false;
    public bool IsAssistant => false;
}

public sealed class SessionAccess(AppDbContext db, ICurrentUser user) : ISessionAccess
{
    public void EnsureProfessor()
    {
        if (!user.IsProfessor) throw new UnauthorizedAccessException("Samo profesor može da obriše sesiju.");
    }

    public void SetOwner(ExamSession session)
    {
        if (user.Id is not { } id || (!user.IsProfessor && !user.IsAssistant)) throw new UnauthorizedAccessException();
        session.SetOwner(id);
    }

    public async Task EnsureAccessAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (user.IsProfessor) return;
        if (!user.IsAssistant || user.Id is null || !await db.ExamSessions.AnyAsync(s => s.Id == sessionId && s.OwnerUserId == user.Id, cancellationToken))
            throw new UnauthorizedAccessException("Nemate pristup ovoj sesiji.");
    }

    public async Task<IReadOnlySet<Guid>?> AccessibleIdsAsync(CancellationToken cancellationToken = default)
    {
        if (user.IsProfessor) return null;
        if (!user.IsAssistant || user.Id is null) throw new UnauthorizedAccessException();
        return (await db.ExamSessions.Where(s => s.OwnerUserId == user.Id).Select(s => s.Id).ToListAsync(cancellationToken)).ToHashSet();
    }

    public async Task PrepareStartAsync(ExamSession session, CancellationToken cancellationToken = default)
    {
        if (session.OwnerUserId is not { } ownerId) return; // Historical professor sessions.
        var owner = await db.Users.SingleAsync(u => u.Id == ownerId, cancellationToken);
        var devices = await db.StaffDevices.Where(d => d.UserId == ownerId).ToListAsync(cancellationToken);
        if (owner.Role == "Assistant" && devices.Count == 0)
            throw new InvalidOperationException("Pre pokretanja registrujte svoje uređaje kroz kratko skeniranje na stranici Moji uređaji.");
        if (await db.ExamSessions.AnyAsync(s => s.OwnerUserId == ownerId && s.Status == "active" && s.RegistrationExpiresAt > DateTimeOffset.UtcNow, cancellationToken))
            throw new InvalidOperationException("Prvo završite registraciono skeniranje na stranici Moji uređaji.");

        // Serializes changes to registrations against session start via optimistic concurrency.
        owner.TouchStaffDevices();
        foreach (var device in devices)
            db.WhitelistEntries.Add(WhitelistEntry.ForStaffDevice(session.Id.ToString(), device, owner.Username, DateTimeOffset.UtcNow));
    }
}
