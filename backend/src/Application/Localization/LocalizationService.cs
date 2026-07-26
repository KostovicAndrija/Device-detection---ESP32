namespace Application.Localization;

public sealed class LocalizationService : ILocalizationService
{
    public LocationEstimateDto? Estimate(IReadOnlyCollection<SensorReadingDto> readings)
    {
        if (readings.Count == 0)
        {
            return null;
        }

        var weighted = readings
            .Select(x =>
            {
                var weight = Math.Clamp(100 - Math.Abs(x.Rssi), 1, 100);
                return new { x.SensorX, x.SensorY, Weight = weight };
            })
            .ToList();

        var totalWeight = weighted.Sum(x => x.Weight);
        if (totalWeight <= 0)
        {
            return null;
        }

        var x = weighted.Sum(v => v.SensorX * v.Weight) / totalWeight;
        var y = weighted.Sum(v => v.SensorY * v.Weight) / totalWeight;
        var confidence = Math.Clamp(totalWeight / (readings.Count * 100.0), 0.0, 1.0);

        return new LocationEstimateDto(Math.Round(x, 3), Math.Round(y, 3), Math.Round(confidence, 3));
    }
}
