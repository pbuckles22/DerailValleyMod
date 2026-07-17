using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BrakePipeDisplayTests
{
    [Fact]
    public void FormatBar_shows_placeholder_when_missing()
    {
        Assert.Equal("— bar", BrakePipeDisplay.FormatBar(null));
    }

    [Theory]
    [InlineData(0f, "0.0 bar")]
    [InlineData(5f, "5.0 bar")]
    [InlineData(4.96f, "5.0 bar")]
    [InlineData(4.94f, "4.9 bar")]
    public void FormatBar_rounds_to_one_decimal(float pressureBar, string expected)
    {
        Assert.Equal(expected, BrakePipeDisplay.FormatBar(pressureBar));
    }
}
