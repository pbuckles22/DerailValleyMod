using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Tier 1 — hitch attribution must name the hot section for CI (no Unity).
/// </summary>
public class HitchSectionBudgetTests
{
    [Fact]
    public void FormatSpike_names_hottest_section()
    {
        var line = HitchSectionBudget.FormatSpike(
            frameDtMs: 120f,
            ("lookAt", 5f),
            ("hudBuild", 4f),
            ("onGui", 95f),
            ("locoRadar", 0f),
            ("limitFilo", 0f));

        Assert.Contains("T2 hitch-spike:", line);
        Assert.Contains("dt=120ms", line);
        Assert.Contains("hot=onGui", line);
        Assert.Contains("onGui=95ms", line);
    }

    [Fact]
    public void FormatSpike_marks_outside_when_frame_exceeds_measured()
    {
        var line = HitchSectionBudget.FormatSpike(
            frameDtMs: 200f,
            ("lookAt", 2f),
            ("hudBuild", 3f),
            ("onGui", 4f),
            ("locoRadar", 0f),
            ("limitFilo", 0f));

        Assert.Contains("hot=outside", line);
        Assert.Contains("outside=", line);
    }

    [Fact]
    public void FormatSpike_null_when_under_frame_threshold()
    {
        Assert.Null(
            HitchSectionBudget.FormatSpike(
                frameDtMs: 20f,
                ("onGui", 15f)));
    }

    [Fact]
    public void PickHottest_ignores_blank_and_picks_max()
    {
        Assert.Equal(
            "locoRadar",
            HitchSectionBudget.PickHottest(
                ("lookAt", 1f),
                ("locoRadar", 40f),
                ("", 99f),
                ("onGui", 10f)));
    }
}
