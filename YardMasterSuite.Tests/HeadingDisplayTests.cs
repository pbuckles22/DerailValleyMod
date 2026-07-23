using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HeadingDisplayTests
{
    private const float Eps = 0.01f;

    [Theory]
    [InlineData(0f, 1f, 0f)] // +Z north
    [InlineData(1f, 0f, 90f)] // +X east
    [InlineData(0f, -1f, 180f)] // −Z south
    [InlineData(-1f, 0f, 270f)] // −X west
    public void FromForward_cardinal_axes(float x, float z, float expected)
    {
        var heading = HeadingDisplay.FromForward(x, z);
        Assert.NotNull(heading);
        Assert.InRange(heading!.Value, expected - Eps, expected + Eps);
    }

    [Fact]
    public void FromForward_zero_length_is_null()
    {
        Assert.Null(HeadingDisplay.FromForward(0f, 0f));
    }

    [Fact]
    public void Format_null_is_placeholder()
    {
        Assert.Equal("— Heading", HeadingDisplay.Format((float?)null));
    }

    [Theory]
    [InlineData(0f, "Heading N")]
    [InlineData(11f, "Heading N")]
    [InlineData(12f, "Heading NNE")]
    [InlineData(22.5f, "Heading NNE")]
    [InlineData(45f, "Heading NE")]
    [InlineData(67.5f, "Heading ENE")]
    [InlineData(90f, "Heading E")]
    [InlineData(112.5f, "Heading ESE")]
    [InlineData(135f, "Heading SE")]
    [InlineData(157.5f, "Heading SSE")]
    [InlineData(180f, "Heading S")]
    [InlineData(202.5f, "Heading SSW")]
    [InlineData(225f, "Heading SW")]
    [InlineData(247.5f, "Heading WSW")]
    [InlineData(270f, "Heading W")]
    [InlineData(292.5f, "Heading WNW")]
    [InlineData(315f, "Heading NW")]
    [InlineData(337.5f, "Heading NNW")]
    [InlineData(359f, "Heading N")]
    public void Format_sixteen_point_compass(float degrees, string expected)
    {
        Assert.Equal(expected, HeadingDisplay.Format(degrees));
    }

    [Theory]
    [InlineData(0f, "N")]
    [InlineData(45f, "NE")]
    [InlineData(67.5f, "ENE")]
    public void ToCompassPoint_maps(float degrees, string expected)
    {
        Assert.Equal(expected, HeadingDisplay.ToCompassPoint(degrees));
    }

    [Fact]
    public void ToCompassPoint_null_is_null()
    {
        Assert.Null(HeadingDisplay.ToCompassPoint(null));
    }
}
