using Application.Risk;

namespace UnitTests;

public sealed class RiskScoringServiceTests
{
    [Fact]
    public void Calculate_ReturnsHigherScore_ForUnknownAndNotWhitelistedDevice()
    {
        var service = new RiskScoringService();

        var score = service.Calculate(rssi: -45, whitelisted: false, unknownDevice: true);

        Assert.InRange(score, 70, 100);
    }

    [Fact]
    public void Calculate_ReturnsLowerScore_WhenWhitelisted()
    {
        var service = new RiskScoringService();

        var score = service.Calculate(rssi: -45, whitelisted: true, unknownDevice: false);

        Assert.InRange(score, 0, 70);
    }
}
