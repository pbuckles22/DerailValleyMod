using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedDisplayTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(10f, 36f)]
    [InlineData(27.777778f, 100f)]
    public void ToKilometersPerHour_converts_meters_per_second(float metersPerSecond, float expectedKmh)
    {
        Assert.Equal(expectedKmh, SpeedDisplay.ToKilometersPerHour(metersPerSecond), precision: 2);
    }

    [Fact]
    public void FormatKmh_rounds_to_whole_kilometers()
    {
        Assert.Equal("36 km/h", SpeedDisplay.FormatKmh(36.4f));
        Assert.Equal("37 km/h", SpeedDisplay.FormatKmh(36.5f));
    }

    [Fact]
    public void FormatFromMetersPerSecond_shows_placeholder_when_missing()
    {
        Assert.Equal("— km/h", SpeedDisplay.FormatFromMetersPerSecond(null));
    }

    [Fact]
    public void FormatFromMetersPerSecond_formats_converted_speed()
    {
        Assert.Equal("36 km/h", SpeedDisplay.FormatFromMetersPerSecond(10f));
    }
}
