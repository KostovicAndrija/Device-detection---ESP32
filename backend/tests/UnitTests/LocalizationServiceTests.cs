using Application.Localization;

namespace UnitTests;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void Estimate_ReturnsWeightedCentroid()
    {
        var service = new LocalizationService();
        var readings = new[]
        {
            new SensorReadingDto("S1", 0, 0, -40),
            new SensorReadingDto("S2", 4, 0, -80),
            new SensorReadingDto("S3", 0, 4, -80)
        };

        var estimate = service.Estimate(readings);

        Assert.NotNull(estimate);
        Assert.InRange(estimate!.X, 0, 2);
        Assert.InRange(estimate.Y, 0, 2);
        Assert.InRange(estimate.Confidence, 0.0, 1.0);
    }

    [Fact]
    public void Estimate_ReturnsNull_WhenNoReadings()
    {
        var service = new LocalizationService();

        var estimate = service.Estimate(Array.Empty<SensorReadingDto>());

        Assert.Null(estimate);
    }
}
