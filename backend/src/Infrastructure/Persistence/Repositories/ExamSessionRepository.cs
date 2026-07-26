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
}
