namespace Application.Risk;

public sealed class RiskScoringService : IRiskScoringService
{
    public int Calculate(double rssi, bool whitelisted, bool unknownDevice, string signalType = "wifi")
    {
        var proximityPart = (int)Math.Clamp(Math.Round(100 - Math.Abs(rssi)), 0, 100);
        var unknownPart = unknownDevice ? 20 : 0;
        var whitelistPart = whitelisted ? -30 : 0;
        var pairingPart = signalType.Contains("pair", StringComparison.OrdinalIgnoreCase) ? 30 : 0;
        var bluetoothPart = signalType.Contains("bluetooth", StringComparison.OrdinalIgnoreCase) ||
                            signalType.Contains("ble", StringComparison.OrdinalIgnoreCase) ? 5 : 0;

        return (int)Math.Clamp(proximityPart + unknownPart + whitelistPart + pairingPart + bluetoothPart, 0, 100);
    }
}
