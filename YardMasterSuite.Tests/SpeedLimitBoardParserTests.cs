using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitBoardParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("200")]
    public void ParseKmh_unknown_for_invalid(string? text)
    {
        Assert.Null(SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("6", 60f)]
    [InlineData("8", 80f)]
    [InlineData("12", 120f)]
    [InlineData("1", 10f)]
    public void ParseKmh_digits_times_ten(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("30", 30f)]
    [InlineData("60", 60f)]
    [InlineData("80", 80f)]
    [InlineData("100", 100f)]
    [InlineData("120", 120f)]
    public void ParseKmh_full_kmh_passthrough(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Fact]
    public void ParseKmh_uses_first_line_and_slash_left()
    {
        Assert.Equal(80f, SpeedLimitBoardParser.ParseKmh("8\nextra"));
        Assert.Equal(60f, SpeedLimitBoardParser.ParseKmh("6/4"));
    }
}
