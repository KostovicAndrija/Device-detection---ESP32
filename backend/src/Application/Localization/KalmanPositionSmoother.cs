using System.Collections.Concurrent;

namespace Application.Localization;

public sealed class KalmanPositionSmoother : IPositionSmoother
{
    private readonly ConcurrentDictionary<Guid, State> _states = new();

    public LocationEstimateDto Smooth(Guid deviceId, LocationEstimateDto measurement)
    {
        var state = _states.GetOrAdd(deviceId, _ => new State(measurement.X, measurement.Y));
        lock (state)
        {
            state.X = Update(state.X, measurement.X, ref state.ErrorX);
            state.Y = Update(state.Y, measurement.Y, ref state.ErrorY);
            return measurement with { X = Math.Round(state.X, 3), Y = Math.Round(state.Y, 3) };
        }
    }

    private static double Update(double estimate, double measurement, ref double error)
    {
        const double processNoise = 0.01;
        const double measurementNoise = 2.0;
        error += processNoise;
        var gain = error / (error + measurementNoise);
        var result = estimate + gain * (measurement - estimate);
        error *= 1 - gain;
        return result;
    }

    private sealed class State(double x, double y)
    {
        public double X = x;
        public double Y = y;
        public double ErrorX = 1;
        public double ErrorY = 1;
    }
}
