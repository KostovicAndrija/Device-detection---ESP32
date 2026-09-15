using Domain.Entities;

namespace Application.Abstractions.Security;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsProfessor { get; }
    bool IsAssistant { get; }
}

public interface ISessionAccess
{
    void SetOwner(ExamSession session);
    Task EnsureAccessAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>?> AccessibleIdsAsync(CancellationToken cancellationToken = default);
    Task PrepareStartAsync(ExamSession session, CancellationToken cancellationToken = default);
}
