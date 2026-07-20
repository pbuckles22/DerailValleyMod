using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LoadDisplayTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0f, 0f)]
    [InlineData(100f, 0f)]
    [InlineData(null, 1000f)]
    public void PercentFromAmps_unknown_when_inputs_invalid(float? current, float? max)
    {
        Assert.Null(LoadDisplay.PercentFromAmps(current, max));
    }

    [Fact]
    public void PercentFromAmps_is_current_over_max()
    {
        Assert.Equal(42f, LoadDisplay.PercentFromAmps(420f, 1000f));
        Assert.Equal(100f, LoadDisplay.PercentFromAmps(1000f, 1000f));
        Assert.Equal(0f, LoadDisplay.PercentFromAmps(0f, 1000f));
    }

    [Fact]
    public void PercentFromAmps_clamps_above_100()
    {
        Assert.Equal(100f, LoadDisplay.PercentFromAmps(1200f, 1000f));
    }

    [Fact]
    public void PercentFromNormalized_scales_0_1_to_percent()
    {
        Assert.Null(LoadDisplay.PercentFromNormalized(null));
        Assert.Equal(0f, LoadDisplay.PercentFromNormalized(0f));
        Assert.Equal(42f, LoadDisplay.PercentFromNormalized(0.42f));
        Assert.Equal(100f, LoadDisplay.PercentFromNormalized(1.2f));
    }

    [Fact]
    public void Format_shows_placeholder_and_whole_percent()
    {
        Assert.Equal("— Load", LoadDisplay.Format(null));
        Assert.Equal("Load 0 %", LoadDisplay.Format(0f));
        Assert.Equal("Load 42 %", LoadDisplay.Format(42.4f));
        Assert.Equal("Load 80 %", LoadDisplay.Format(79.6f));
    }

    [Fact]
    public void Format_plain_has_no_color_tags()
    {
        Assert.Equal("Load 85 %", LoadDisplay.Format(85f));
        Assert.Equal("Load 96 %", LoadDisplay.Format(96f));
    }

    [Fact]
    public void FormatHud_yellow_at_80_and_red_at_95()
    {
        Assert.Equal("Load 79 %", LoadDisplay.FormatHud(79f));
        Assert.Equal(
            $"<color={LoadDisplay.WarningColor}>Load 80 %</color>",
            LoadDisplay.FormatHud(80f));
        Assert.Equal(
            $"<color={LoadDisplay.WarningColor}>Load 94 %</color>",
            LoadDisplay.FormatHud(94f));
        Assert.Equal(
            $"<color={LoadDisplay.CriticalColor}>Load 95 %</color>",
            LoadDisplay.FormatHud(95f));
        Assert.Equal(
            $"<color={LoadDisplay.CriticalColor}>Load 100 %</color>",
            LoadDisplay.FormatHud(100f));
    }

    [Fact]
    public void FormatHud_placeholder_is_plain()
    {
        Assert.Equal("— Load", LoadDisplay.FormatHud(null));
    }
}
