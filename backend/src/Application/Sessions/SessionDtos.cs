namespace Application.Sessions;

public sealed record CreateSessionRequest(string Name, string RoomId, DateTimeOffset StartsAt);

public sealed record StartRoomSessionRequest(string RoomId, string? Name = null);

public sealed record SessionDto(Guid Id, string Name, string RoomId, DateTimeOffset StartsAt, DateTimeOffset? EndsAt, string Status);
