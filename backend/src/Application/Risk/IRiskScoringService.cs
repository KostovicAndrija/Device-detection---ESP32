namespace Application.Risk;

public interface IRiskScoringService
{
    int Calculate(double rssi, bool whitelisted, bool unknownDevice, string signalType = "wifi");
}
