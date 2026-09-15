namespace Application.Whitelist;

public sealed record CreateWhitelistRequest(
    string SessionId,
    string StudentRef,
    string DeviceIdentifier,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record WhitelistEntryDto(
    Guid Id,
    string SessionId,
    string StudentRef,
    string DeviceHash,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    Guid? StaffDeviceId = null);
