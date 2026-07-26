using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArScreenClusterLayoutTests
{
    [Fact]
    public void PackNonOverlapping_leaves_separated_boxes_alone()
    {
        var xs = new[] { 100f, 400f };
        var ys = new[] { 200f, 200f };
        var hw = new[] { 30f, 30f };
        var hh = new[] { 30f, 30f };
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 2, gapPixels: 8f);
        Assert.Equal(100f, xs[0]);
        Assert.Equal(400f, xs[1]);
    }

    [Fact]
    public void PackNonOverlapping_two_stacked_squares_sit_side_by_side()
    {
        var xs = new[] { 300f, 302f };
        var ys = new[] { 180f, 181f };
        var hw = new[] { 40f, 40f };
        var hh = new[] { 40f, 40f };
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 2, gapPixels: 8f);
        Assert.Equal(257f, xs[0], 0.01);
        Assert.Equal(345f, xs[1], 0.01);
        Assert.False(ArScreenClusterLayout.BoxesOverlap(xs, ys, hw, hh, 0, 1, 8f));
        Assert.Equal(
            ArScreenClusterLayout.RequiredCenterDistance(40f, 40f, 8f),
            xs[1] - xs[0],
            0.01);
    }

    [Fact]
    public void PackNonOverlapping_edge_fit_preserves_gap_not_venn()
    {
        // Both centers near left edge — old per-box clamp crushed them into a Venn.
        var xs = new[] { 20f, 25f };
        var ys = new[] { 50f, 50f };
        var hw = new[] { 50f, 70f };
        var hh = new[] { 40f, 40f };
        ArScreenClusterLayout.PackNonOverlapping(
            xs,
            ys,
            hw,
            hh,
            2,
            gapPixels: 8f,
            screenWidth: 800f,
            edgeMargin: 28f);

        Assert.Equal(
            ArScreenClusterLayout.RequiredCenterDistance(50f, 70f, 8f),
            xs[1] - xs[0],
            0.01);
        Assert.False(ArScreenClusterLayout.BoxesOverlap(xs, ys, hw, hh, 0, 1, 8f));
        Assert.True(xs[0] - hw[0] >= 28f - 0.01f);
        Assert.True(xs[1] + hw[1] <= 800f - 28f + 0.01f);
    }

    [Fact]
    public void PackNonOverlapping_respects_unequal_widths()
    {
        var xs = new[] { 200f, 205f };
        var ys = new[] { 100f, 100f };
        var hw = new[] { 20f, 50f };
        var hh = new[] { 20f, 20f };
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 2, gapPixels: 10f);
        Assert.True(xs[0] < xs[1]);
        Assert.False(ArScreenClusterLayout.BoxesOverlap(xs, ys, hw, hh, 0, 1, 10f));
        Assert.Equal(80f, xs[1] - xs[0], 0.01);
    }

    [Fact]
    public void PackNonOverlapping_two_clusters_independent()
    {
        var xs = new[] { 100f, 105f, 500f, 505f };
        var ys = new[] { 50f, 50f, 50f, 50f };
        var hw = new[] { 30f, 30f, 30f, 30f };
        var hh = new[] { 30f, 30f, 30f, 30f };
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 4, 8f);
        Assert.True(xs[0] < xs[1]);
        Assert.True(xs[2] < xs[3]);
        Assert.True(xs[1] < 250f);
        Assert.True(xs[2] > 400f);
    }

    [Fact]
    public void PackNonOverlapping_noop_for_zero_or_one()
    {
        var xs = new[] { 10f };
        var ys = new[] { 20f };
        var hw = new[] { 5f };
        var hh = new[] { 5f };
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 1, 8f);
        Assert.Equal(10f, xs[0]);
        ArScreenClusterLayout.PackNonOverlapping(xs, ys, hw, hh, 0, 8f);
    }

    [Fact]
    public void BoxesOverlap_detects_venn_and_gap()
    {
        var xs = new[] { 100f, 150f };
        var ys = new[] { 100f, 100f };
        var hw = new[] { 40f, 40f };
        var hh = new[] { 40f, 40f };
        Assert.True(ArScreenClusterLayout.BoxesOverlap(xs, ys, hw, hh, 0, 1, 8f));
        xs[1] = 200f;
        Assert.False(ArScreenClusterLayout.BoxesOverlap(xs, ys, hw, hh, 0, 1, 8f));
    }

    [Fact]
    public void FitClusterSpanInView_shifts_rigidly()
    {
        var xs = new[] { 10f, 138f }; // already 50+8+70 apart
        var hw = new[] { 50f, 70f };
        var members = new[] { 0, 1 };
        ArScreenClusterLayout.FitClusterSpanInView(xs, hw, members, 2, viewMin: 28f, viewMax: 772f);
        Assert.Equal(128f, xs[1] - xs[0], 0.01);
        Assert.Equal(28f, xs[0] - hw[0], 0.01);
    }
}
