using Application.Abstractions.Persistence;

namespace Infrastructure.Persistence;

public sealed class AppUnitOfWork(AppDbContext dbContext) : IAppUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
