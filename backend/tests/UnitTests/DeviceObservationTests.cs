using Domain.Entities;

namespace UnitTests;

public sealed class DeviceObservationTests
{
    [Fact]
    public void Create_AllowsSmallSensorClockSkew()
    {
        var observation = DeviceObservation.Create(
            Guid.NewGuid(), "S1", null, "wifi", -65,
            DateTimeOffset.UtcNow.AddMinutes(4));

        Assert.Equal("S1", observation.SensorId);
    }

    [Fact]
    public void Create_RejectsTimestampFarInFuture()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DeviceObservation.Create(
            Guid.NewGuid(), "S1", null, "wifi", -65,
            DateTimeOffset.UtcNow.AddMinutes(10)));
    }
}
