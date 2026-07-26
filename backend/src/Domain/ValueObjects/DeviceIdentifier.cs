namespace Domain.ValueObjects;

public readonly record struct DeviceIdentifier(string Value)
{
    public static DeviceIdentifier Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Device identifier is required.", nameof(value));
        }

        return new DeviceIdentifier(value.Trim());
    }
}
