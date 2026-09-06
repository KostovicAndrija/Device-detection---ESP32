namespace Application.Localization;

public sealed record DevicePositionDto(
    Guid DeviceId,
    double X,
    double Y,
    double Confidence,
    DateTimeOffset CapturedAt,
    int SensorCount);

public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";
    public int WindowSeconds { get; set; } = 15;
    public List<SensorPositionOptions> Sensors { get; set; } =
    [
        new() { Id = "S1", X = 0, Y = 0 },
        new() { Id = "S2", X = 8, Y = 0 },
        new() { Id = "S3", X = 4, Y = 6 }
    ];
}

public sealed class SensorPositionOptions
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
