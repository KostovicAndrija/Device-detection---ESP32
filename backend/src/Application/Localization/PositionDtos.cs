namespace Application.Localization;

public sealed record DevicePositionDto(
    Guid DeviceId,
    double X,
    double Y,
    double Confidence,
    DateTimeOffset CapturedAt,
    int SensorCount,
    bool IsWhitelisted,
    string SignalType = "unknown");

public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";
    public int WindowSeconds { get; set; } = 15;
    public List<SensorPositionOptions> Sensors { get; set; } = [];
}

public sealed class SensorPositionOptions
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
}
