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

    [Fact]
    public void Calculate_RaisesScore_ForBluetoothPairingAttempt()
    {
        var service = new RiskScoringService();

        var regular = service.Calculate(-70, whitelisted: false, unknownDevice: false, "bluetooth");
        var pairing = service.Calculate(-70, whitelisted: false, unknownDevice: false, "bluetooth_pairing");

        Assert.True(pairing > regular);
    }
}
