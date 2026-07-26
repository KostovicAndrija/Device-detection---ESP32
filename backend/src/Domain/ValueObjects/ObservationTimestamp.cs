namespace Domain.ValueObjects;

public readonly record struct ObservationTimestamp(DateTimeOffset Value)
{
    public static ObservationTimestamp Create(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentException("Observation timestamp is required.", nameof(value));
        }

        return new ObservationTimestamp(value);
    }
}
