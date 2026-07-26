namespace Application.Abstractions.Persistence;

public interface IAppUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
