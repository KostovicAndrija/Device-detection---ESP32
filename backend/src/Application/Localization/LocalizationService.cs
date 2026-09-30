namespace Application.Localization;

public sealed class LocalizationService : ILocalizationService
{

    public LocationEstimateDto? Estimate(IReadOnlyCollection<SensorReadingDto> readings)
    {
        if (readings.Count == 0)
        {
            return null;
        }

        if (readings.Count < 3)
        {
            return WeightedCentroid(readings);
        }

        var measured = readings
            .Select(x => new
            {
                Reading = x,
                Distance = Math.Pow(10, (x.ReferenceRssi - x.Rssi) / (10 * x.PathLossExponent)),
                Weight = Math.Pow(10, (x.Rssi + 100) / 20)
            })
            .OrderByDescending(x => x.Weight)
            .ToList();

        var reference = measured[0];
        double a11 = 0, a12 = 0, a22 = 0, b1 = 0, b2 = 0;

        foreach (var item in measured.Skip(1))
        {
            var ax = 2 * (item.Reading.SensorX - reference.Reading.SensorX);
            var ay = 2 * (item.Reading.SensorY - reference.Reading.SensorY);
            var b = Math.Pow(reference.Distance, 2) - Math.Pow(item.Distance, 2)
                    - Math.Pow(reference.Reading.SensorX, 2) + Math.Pow(item.Reading.SensorX, 2)
                    - Math.Pow(reference.Reading.SensorY, 2) + Math.Pow(item.Reading.SensorY, 2);
            var weight = Math.Max(0.001, item.Weight);

            a11 += weight * ax * ax;
            a12 += weight * ax * ay;
            a22 += weight * ay * ay;
            b1 += weight * ax * b;
            b2 += weight * ay * b;
        }

        var determinant = a11 * a22 - a12 * a12;
        if (Math.Abs(determinant) < 0.000001)
        {
            return WeightedCentroid(readings);
        }

        var x = (b1 * a22 - b2 * a12) / determinant;
        var y = (a11 * b2 - a12 * b1) / determinant;
        x = Math.Clamp(x, readings.Min(r => r.SensorX), readings.Max(r => r.SensorX));
        y = Math.Clamp(y, readings.Min(r => r.SensorY), readings.Max(r => r.SensorY));
        var residual = measured.Average(item =>
            Math.Abs(
                Math.Sqrt(Math.Pow(x - item.Reading.SensorX, 2) + Math.Pow(y - item.Reading.SensorY, 2))
                - item.Distance));
        var confidence = Math.Clamp(1 / (1 + residual), 0, 1);

        return new LocationEstimateDto(Math.Round(x, 3), Math.Round(y, 3), Math.Round(confidence, 3));
    }

    private static LocationEstimateDto? WeightedCentroid(IReadOnlyCollection<SensorReadingDto> readings)
    {
        var weighted = readings
            .Select(x =>
            {
                var weight = Math.Pow(10, (x.Rssi + 100) / 20);
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
        var confidence = Math.Clamp(readings.Count / 3.0 * 0.6, 0.0, 0.6);
        return new LocationEstimateDto(Math.Round(x, 3), Math.Round(y, 3), Math.Round(confidence, 3));
    }
}
