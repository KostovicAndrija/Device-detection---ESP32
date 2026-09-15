namespace Application.Sessions;

public interface IExamSessionService
{
    Task<SessionDto> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SessionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SessionDto?> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SessionDto> StartForRoomAsync(StartRoomSessionRequest request, CancellationToken cancellationToken = default);
    Task<SessionDto?> StopAsync(Guid id, CancellationToken cancellationToken = default);
}
