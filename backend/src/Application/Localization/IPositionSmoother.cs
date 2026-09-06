namespace Application.Localization;

public interface IPositionSmoother
{
    LocationEstimateDto Smooth(Guid deviceId, LocationEstimateDto measurement);
}
