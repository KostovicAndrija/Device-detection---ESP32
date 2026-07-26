namespace Application.Risk;

public sealed class RiskScoringService : IRiskScoringService
{
    public int Calculate(double rssi, bool whitelisted, bool unknownDevice)
    {
        var proximityPart = (int)Math.Clamp(Math.Round(100 - Math.Abs(rssi)), 0, 100);
        var unknownPart = unknownDevice ? 20 : 0;
        var whitelistPart = whitelisted ? -30 : 0;

        return (int)Math.Clamp(proximityPart + unknownPart + whitelistPart, 0, 100);
    }
}
