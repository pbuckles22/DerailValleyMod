using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BrakePipeDisplayTests
{
    [Fact]
    public void FormatBar_shows_placeholder_when_missing()
    {
        Assert.Equal("— Pipe", BrakePipeDisplay.FormatBar(null));
    }

    [Theory]
    [InlineData(0f, "Pipe 0.0 bar")]
    [InlineData(5f, "Pipe 5.0 bar")]
    [InlineData(4.96f, "Pipe 5.0 bar")]
    [InlineData(4.94f, "Pipe 4.9 bar")]
    public void FormatBar_rounds_to_one_decimal(float pressureBar, string expected)
    {
        Assert.Equal(expected, BrakePipeDisplay.FormatBar(pressureBar));
    }
}
