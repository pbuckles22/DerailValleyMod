using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitDisplayTests
{
    [Theory]
    [InlineData(40f, 10f)]
    [InlineData(50f, 20f)]
    [InlineData(69f, 20f)]
    [InlineData(70f, 30f)]
    [InlineData(94f, 30f)]
    [InlineData(95f, 40f)]
    [InlineData(129f, 40f)]
    [InlineData(130f, 50f)]
    [InlineData(169f, 50f)]
    [InlineData(170f, 60f)]
    [InlineData(229f, 60f)]
    [InlineData(230f, 70f)]
    [InlineData(359f, 70f)]
    [InlineData(360f, 80f)]
    [InlineData(699f, 80f)]
    [InlineData(700f, 90f)]
    [InlineData(899f, 90f)]
    [InlineData(900f, 100f)]
    [InlineData(1199f, 100f)]
    [InlineData(1200f, 120f)]
    [InlineData(5000f, 120f)]
    public void MaxSpeedForMinRadius_matches_sign_placer_table(float minRadius, float expectedKmh)
    {
        Assert.Equal(expectedKmh, SpeedLimitGeometry.MaxSpeedForMinRadius(minRadius));
    }

    [Fact]
    public void MaxSpeedForMinRadius_unknown_when_non_finite_or_non_positive()
    {
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(float.NaN));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(float.PositiveInfinity));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(0f));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(-1f));
    }

    [Fact]
    public void Format_shows_placeholder_and_whole_kmh()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.Format(null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.Format(60f));
        Assert.Equal("Limit 10", SpeedLimitDisplay.Format(9.6f));
    }

    [Fact]
    public void Format_plain_has_no_color_tags()
    {
        Assert.Equal("Limit 40", SpeedLimitDisplay.Format(40f));
    }

    [Fact]
    public void FormatHud_yellow_near_limit_and_red_when_over()
    {
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(55f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(56f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(60f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.CriticalColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(61f, 60f));
    }

    [Fact]
    public void FormatHud_placeholder_when_limit_unknown_plain_when_speed_unknown()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.FormatHud(40f, null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(null, 60f));
    }
}
