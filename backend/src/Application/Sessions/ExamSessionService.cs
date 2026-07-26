using Application.Abstractions.Persistence;
using Domain.Entities;

namespace Application.Sessions;

public sealed class ExamSessionService(IExamSessionRepository repository, IAppUnitOfWork unitOfWork) : IExamSessionService
{
    public async Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = ExamSession.Create(request.Name, request.RoomId, request.StartsAt);
        await repository.AddAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(session);
    }

    public async Task<IReadOnlyList<SessionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await repository.GetAllAsync(cancellationToken);
        return sessions.Select(Map).ToList();
    }

    public async Task<SessionDto?> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await repository.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.Start(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(session);
    }

    public async Task<SessionDto?> StopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var session = await repository.GetByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.Stop(DateTimeOffset.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(session);
    }

    private static SessionDto Map(ExamSession session)
        => new(session.Id, session.Name, session.RoomId, session.StartsAt, session.EndsAt, session.Status);
}
