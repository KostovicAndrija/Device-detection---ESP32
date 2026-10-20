namespace Application.Localization;

/// <summary>
/// Configurable criteria used to decide whether a desk is normal, suspicious or critical.
/// Values are loaded from the "DeskRisk" section in appsettings.json.
/// </summary>
public sealed class DeskRiskOptions
{
    public const string SectionName = "DeskRisk";

    public int BucketSeconds { get; set; } = 15;
    public int MinimumSensorCount { get; set; } = 2;
    public double MinimumPositionConfidence { get; set; } = .35;
    public double DeskAssociationRadiusMeters { get; set; } = .75;
    public int WindowBucketCount { get; set; } = 5;
    public int MaximumSensorTimeSpreadSeconds { get; set; } = 5;
    public int WarningThreshold { get; set; } = 40;
    public int CriticalThreshold { get; set; } = 60;

    public bool IsValid() => BucketSeconds is >= 1 and <= 300 && WindowBucketCount is >= 2 and <= 1000 &&
        MinimumSensorCount is >= 2 and <= 10 && MaximumSensorTimeSpreadSeconds >= 0 &&
        MaximumSensorTimeSpreadSeconds <= BucketSeconds && double.IsFinite(MinimumPositionConfidence) &&
        MinimumPositionConfidence is >= 0 and <= 1 && double.IsFinite(DeskAssociationRadiusMeters) &&
        DeskAssociationRadiusMeters is >= 0 and <= 10 &&
        WarningThreshold > 0 && WarningThreshold < CriticalThreshold && CriticalThreshold <= 100;
}
