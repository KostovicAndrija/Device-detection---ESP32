using Application.Localization;

namespace UnitTests;

public sealed class KalmanPositionSmootherTests
{
    [Fact]
    public void Smooth_MovesEstimateGradually()
    {
        var smoother = new KalmanPositionSmoother();
        var id = Guid.NewGuid();
        _ = smoother.Smooth(id, new LocationEstimateDto(0, 0, 1));

        var result = smoother.Smooth(id, new LocationEstimateDto(8, 6, 1));

        Assert.InRange(result.X, 0.1, 7.9);
        Assert.InRange(result.Y, 0.1, 5.9);
    }
}
