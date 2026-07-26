namespace Application.Localization;

public sealed record SensorReadingDto(string SensorId, double SensorX, double SensorY, double Rssi);

public sealed record LocationEstimateDto(double X, double Y, double Confidence);
