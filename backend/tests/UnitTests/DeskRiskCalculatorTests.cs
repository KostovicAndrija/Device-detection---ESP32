using Application.Localization;

namespace UnitTests;

public sealed class DeskRiskCalculatorTests
{
    [Theory]
    [InlineData(0, 0, "normal")]
    [InlineData(1, 20, "normal")]
    [InlineData(2, 40, "warning")]
    [InlineData(3, 60, "critical")]
    [InlineData(5, 100, "critical")]
    public void Presence_in_fixed_window_decides_criticality(int active, int score, string level)
    {
        var hits = Enumerable.Range(0, active).Select(i => ("ble", (long)i));
        var result = DeskRiskCalculator.Calculate(hits, 5, new());
        Assert.Equal(score, result.Score);
        Assert.Equal(level, result.Level);
    }

    [Fact]
    public void Many_devices_or_packets_in_one_interval_cannot_make_a_desk_critical()
    {
        var hits = Enumerable.Repeat(("ble", 0L), 1000);
        var result = DeskRiskCalculator.Calculate(hits, 5, new());
        Assert.Equal(20, result.Score);
        Assert.Equal(1, result.ActiveBuckets);
        Assert.Equal("normal", result.Level);
    }

    [Fact]
    public void Different_signal_types_are_not_added_together()
    {
        (string, long)[] hits = [("wifi", 0), ("wifi", 1), ("ble", 2), ("ble", 3)];
        var result = DeskRiskCalculator.Calculate(hits, 5, new());
        Assert.Equal(40, result.Score);
        Assert.Equal("warning", result.Level);
    }

    [Fact]
    public void Changing_configuration_changes_the_decision()
    {
        (string, long)[] hits = [("ble", 0), ("ble", 1), ("ble", 2)];
        Assert.Equal("critical", DeskRiskCalculator.Calculate(hits, 5, new()).Level);
        Assert.Equal("warning", DeskRiskCalculator.Calculate(hits, 5, new() { CriticalThreshold = 80 }).Level);
        Assert.Equal(30, DeskRiskCalculator.Calculate(hits, 10, new()).Score);
    }

    [Fact]
    public void Rounding_does_not_promote_a_desk_to_critical()
    {
        var result = DeskRiskCalculator.Calculate([("wifi", 0)], 3, new() { WarningThreshold = 20, CriticalThreshold = 34 });
        Assert.Equal("warning", result.Level);
    }

    [Fact]
    public void Invalid_configuration_is_rejected()
    {
        Assert.True(new DeskRiskOptions().IsValid());
        Assert.False(new DeskRiskOptions { BucketSeconds = 0 }.IsValid());
        Assert.False(new DeskRiskOptions { CriticalThreshold = 30 }.IsValid());
        Assert.False(new DeskRiskOptions { MinimumPositionConfidence = double.NaN }.IsValid());
    }
}
