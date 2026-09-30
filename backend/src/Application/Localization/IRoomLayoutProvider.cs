using Domain.Entities;
namespace Application.Localization;

public interface IRoomLayoutProvider
{
    Task CaptureAsync(ExamSession session, CancellationToken ct = default);
    Task<RoomLayout> ForSessionAsync(Guid sessionId, CancellationToken ct = default);
}
