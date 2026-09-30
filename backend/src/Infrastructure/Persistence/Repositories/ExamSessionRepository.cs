using Application.Abstractions.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class ExamSessionRepository(AppDbContext dbContext) : IExamSessionRepository
{
    public Task<ExamSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.ExamSessions.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ExamSession>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.ExamSessions.OrderByDescending(x => x.StartsAt).ToListAsync(cancellationToken);

    public Task AddAsync(ExamSession session, CancellationToken cancellationToken = default)
        => dbContext.ExamSessions.AddAsync(session, cancellationToken).AsTask();

    public Task<bool> HasObservationsAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.DeviceObservations.AnyAsync(x => x.SessionId == id.ToString(), cancellationToken);

    public async Task RemoveWithRelatedDataAsync(ExamSession session, CancellationToken cancellationToken = default)
    {
        var sessionId = session.Id.ToString();
        dbContext.Alerts.RemoveRange(await dbContext.Alerts.Where(x => x.SessionId == sessionId).ToListAsync(cancellationToken));
        dbContext.WhitelistEntries.RemoveRange(await dbContext.WhitelistEntries.Where(x => x.SessionId == sessionId).ToListAsync(cancellationToken));
        dbContext.DeviceObservations.RemoveRange(await dbContext.DeviceObservations.Where(x => x.SessionId == sessionId).ToListAsync(cancellationToken));
        dbContext.ExamSessions.Remove(session);
    }

    public Task<bool> HasActiveSessionInRoomAsync(
        string roomId,
        Guid exceptId,
        CancellationToken cancellationToken = default)
        => dbContext.ExamSessions.AnyAsync(
            x => x.RoomId == roomId && x.Status == "active" && x.Id != exceptId &&
                 (x.RegistrationExpiresAt == null || x.RegistrationExpiresAt > DateTimeOffset.UtcNow),
            cancellationToken);
}
