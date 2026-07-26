namespace Application.Localization;

public interface ILocalizationService
{
    LocationEstimateDto? Estimate(IReadOnlyCollection<SensorReadingDto> readings);
}
