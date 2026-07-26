using Domain.Entities;

namespace Application.Abstractions.Persistence;

public interface IExamSessionRepository
{
    Task<ExamSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamSession>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ExamSession session, CancellationToken cancellationToken = default);
}
