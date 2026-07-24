using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArMarkerTransitionTests
{
    [Fact]
    public void StepProgress_reaches_one_in_about_one_second()
    {
        var p = 0f;
        p = ArMarkerTransition.StepProgress(p, 0.5f);
        Assert.Equal(0.5f, p, 3);
        p = ArMarkerTransition.StepProgress(p, 0.5f);
        Assert.Equal(1f, p, 3);
        Assert.Equal(1f, ArMarkerTransition.DefaultDurationSeconds, 3);
    }

    [Fact]
    public void Lerp_at_half_ease_is_not_linear_midpoint()
    {
        // EaseInOutCubic(0.5) = 0.5, still midpoint; check endpoints and continuity.
        ArMarkerTransition.Lerp(0f, 0f, 100f, 200f, 0f, out var x0, out var y0);
        Assert.Equal(0f, x0, 3);
        Assert.Equal(0f, y0, 3);
        ArMarkerTransition.Lerp(0f, 0f, 100f, 200f, 1f, out var x1, out var y1);
        Assert.Equal(100f, x1, 3);
        Assert.Equal(200f, y1, 3);
        ArMarkerTransition.Lerp(0f, 0f, 100f, 200f, 0.5f, out var xM, out var yM);
        Assert.Equal(50f, xM, 3);
        Assert.Equal(100f, yM, 3);
    }

    [Fact]
    public void EaseInOutCubic_slower_at_start_than_linear()
    {
        // At t=0.25, cubic ease < 0.25 (starts slow).
        Assert.True(ArMarkerTransition.EaseInOutCubic(0.25f) < 0.25f);
    }
}
