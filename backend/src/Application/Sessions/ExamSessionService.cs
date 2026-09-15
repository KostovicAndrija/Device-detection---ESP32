using Application.Abstractions.Persistence;
using Application.Monitoring;
using Domain.Entities;
using Application.Abstractions.Security;

namespace Application.Sessions;

public sealed class ExamSessionService(
    IExamSessionRepository repository,
    IAppUnitOfWork unitOfWork,
    IMonitoringEventPublisher monitoringEventPublisher,
    ISessionAccess access) : IExamSessionService
{
    public async Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = ExamSession.Create(request.Name, request.RoomId, request.StartsAt);
        access.SetOwner(session);
        await repository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(session);
    }

    public async Task<IReadOnlyList<SessionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await repository.GetAllAsync(cancellationToken);
        var ids = await access.AccessibleIdsAsync(cancellationToken);
        return sessions.Where(s => s.RegistrationExpiresAt is null && (ids is null || ids.Contains(s.Id))).Select(Map).ToList();
    }

    public async Task<SessionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await access.EnsureAccessAsync(id, cancellationToken);
        var session = await repository.GetByIdAsync(id, cancellationToken);
        return session is null ? null : Map(session);
    }

    public async Task<SessionDto?> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await access.EnsureAccessAsync(id, cancellationToken);
        var session = await repository.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.RegistrationExpiresAt is not null) throw new InvalidOperationException("Registracionim skeniranjem upravlja se na stranici Moji uređaji.");

        if (await repository.HasActiveSessionInRoomAsync(session.RoomId, session.Id, cancellationToken))
        {
            throw new InvalidOperationException("Another session is already active in this room.");
        }

        if (session.Status != "planned") throw new InvalidOperationException("Samo planirana sesija može da se pokrene.");
        await access.PrepareStartAsync(session, cancellationToken);
        session.Start(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await monitoringEventPublisher.PublishSessionStateChangedAsync(
            new SessionStateChangedEvent(session.Id, session.Status, DateTimeOffset.UtcNow),
            cancellationToken);
        return Map(session);
    }

    public async Task<SessionDto> StartForRoomAsync(
        StartRoomSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var roomId = request.RoomId?.Trim();
        if (string.IsNullOrWhiteSpace(roomId))
        {
            throw new ArgumentException("Room id is required.", nameof(request));
        }

        if (await repository.HasActiveSessionInRoomAsync(roomId, Guid.Empty, cancellationToken))
        {
            throw new InvalidOperationException("A session is already active in this room.");
        }

        var now = DateTimeOffset.UtcNow;
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"Očitavanje {roomId} · {now:dd.MM.yyyy HH:mm}"
            : request.Name.Trim();
        var session = ExamSession.Create(name, roomId, now);
        access.SetOwner(session);
        await access.PrepareStartAsync(session, cancellationToken);
        session.Start(now);
        await repository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await monitoringEventPublisher.PublishSessionStateChangedAsync(
            new SessionStateChangedEvent(session.Id, session.Status, now),
            cancellationToken);
        return Map(session);
    }

    public async Task<SessionDto?> StopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await access.EnsureAccessAsync(id, cancellationToken);
        var session = await repository.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.Stop(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await monitoringEventPublisher.PublishSessionStateChangedAsync(
            new SessionStateChangedEvent(session.Id, session.Status, DateTimeOffset.UtcNow),
            cancellationToken);
        return Map(session);
    }

    private static SessionDto Map(ExamSession session)
        => new(session.Id, session.Name, session.RoomId, session.StartsAt, session.EndsAt, session.Status);
}
