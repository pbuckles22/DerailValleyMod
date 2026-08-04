using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ReverserDisplayTests
{
    [Theory]
    [InlineData(null, "—")]
    [InlineData(0.5f, "N")]
    [InlineData(0f, "R")]
    [InlineData(1f, "F")]
    [InlineData(0.49f, "R")]
    [InlineData(0.51f, "F")]
    public void Format_plain_letter(float? value, string expected)
    {
        Assert.Equal(expected, ReverserDisplay.Format(value));
    }

    [Fact]
    public void FormatHud_colors_r_n_f()
    {
        Assert.Equal(
            $"<color={ReverserDisplay.ReverseColor}>R</color>",
            ReverserDisplay.FormatHud(0f));
        Assert.Equal(
            $"<color={ReverserDisplay.NeutralColor}>N</color>",
            ReverserDisplay.FormatHud(0.5f));
        Assert.Equal(
            $"<color={ReverserDisplay.ForwardColor}>F</color>",
            ReverserDisplay.FormatHud(1f));
        Assert.Equal("—", ReverserDisplay.FormatHud(null));
    }
}
