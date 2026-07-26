namespace Application.Devices;

public sealed record DeviceDto(Guid Id, string HashId, string Type, DateTimeOffset FirstSeenAt, DateTimeOffset LastSeenAt);
