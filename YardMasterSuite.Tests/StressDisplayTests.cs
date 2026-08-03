using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class StressDisplayTests
{
    [Fact]
    public void PercentOfThreshold_null_when_no_usable_pairs()
    {
        Assert.Null(StressDisplay.PercentOfThreshold(null, null, null, null));
        Assert.Null(StressDisplay.PercentOfThreshold(10f, null, null, null));
        Assert.Null(StressDisplay.PercentOfThreshold(10f, 0.01f, null, null));
        Assert.Null(StressDisplay.PercentOfThreshold(null, 100f, 5f, 0.01f));
    }

    [Fact]
    public void PercentOfThreshold_is_stress_over_threshold()
    {
        Assert.Equal(10f, StressDisplay.PercentOfThreshold(50f, 500f, null, null));
        Assert.Equal(100f, StressDisplay.PercentOfThreshold(500f, 500f, null, null));
        Assert.Equal(200f, StressDisplay.PercentOfThreshold(1000f, 500f, null, null));
    }

    [Fact]
    public void PercentOfThreshold_takes_worse_of_stress_and_build()
    {
        // stress 10%, build 40% → 40
        Assert.Equal(40f, StressDisplay.PercentOfThreshold(50f, 500f, 40f, 100f));
        // stress 90%, build 10% → 90
        Assert.Equal(90f, StressDisplay.PercentOfThreshold(450f, 500f, 10f, 100f));
    }

    [Fact]
    public void Format_shows_placeholder_and_whole_percent()
    {
        Assert.Equal("— Stress", StressDisplay.Format(null));
        Assert.Equal("Stress 0 %", StressDisplay.Format(0f));
        Assert.Equal("Stress 40 %", StressDisplay.Format(40.4f));
        Assert.Equal("Stress 80 %", StressDisplay.Format(79.6f));
        Assert.Equal("Stress 200 %", StressDisplay.Format(200f));
    }

    [Fact]
    public void Format_plain_has_no_color_tags()
    {
        Assert.Equal("Stress 85 %", StressDisplay.Format(85f));
        Assert.Equal("Stress 96 %", StressDisplay.Format(96f));
    }

    [Fact]
    public void FormatHud_rag_green_yellow_red()
    {
        Assert.Equal("— Stress", StressDisplay.FormatHud(null));
        Assert.Equal(
            $"<color={StressDisplay.OkColor}>Stress 40 %</color>",
            StressDisplay.FormatHud(40f));
        Assert.Equal(
            $"<color={StressDisplay.OkColor}>Stress 79 %</color>",
            StressDisplay.FormatHud(79f));
        Assert.Equal(
            $"<color={StressDisplay.WarningColor}>Stress 80 %</color>",
            StressDisplay.FormatHud(80f));
        Assert.Equal(
            $"<color={StressDisplay.WarningColor}>Stress 94 %</color>",
            StressDisplay.FormatHud(94f));
        Assert.Equal(
            $"<color={StressDisplay.CriticalColor}>Stress 95 %</color>",
            StressDisplay.FormatHud(95f));
        Assert.Equal(
            $"<color={StressDisplay.CriticalColor}>Stress 120 %</color>",
            StressDisplay.FormatHud(120f));
    }
}
