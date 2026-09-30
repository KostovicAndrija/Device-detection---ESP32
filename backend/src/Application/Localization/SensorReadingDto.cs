namespace Application.Localization;

public sealed record SensorReadingDto(string SensorId, double SensorX, double SensorY, double Rssi,
    double ReferenceRssi = -45, double PathLossExponent = 2.7);

public sealed record LocationEstimateDto(double X, double Y, double Confidence);
