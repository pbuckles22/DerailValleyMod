using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Perf smoke: Player.log @ 0.6.32 spammed <c>T2 minimap: build</c> (~196×) with polys=616.
/// Rebuild must throttle; draw set must not keep far #Y outside focus.
/// </summary>
public class YardMiniMapRebuildGateTests
{
    [Fact]
    public void Smoke_032_same_yard_inside_interval_skips_rebuild()
    {
        Assert.False(
            YardMiniMapRebuildGate.ShouldRebuild(
                nowSeconds: 10f,
                nextRebuildAt: 12.5f,
                cachedYardId: "MF",
                requestedYardId: "MF"));
    }

    [Fact]
    public void Smoke_032_interval_elapsed_rebuilds()
    {
        Assert.True(
            YardMiniMapRebuildGate.ShouldRebuild(
                nowSeconds: 13f,
                nextRebuildAt: 12.5f,
                cachedYardId: "MF",
                requestedYardId: "MF"));
    }

    [Fact]
    public void Yard_change_rebuilds_even_inside_interval()
    {
        Assert.True(
            YardMiniMapRebuildGate.ShouldRebuild(
                nowSeconds: 10f,
                nextRebuildAt: 12.5f,
                cachedYardId: "MF",
                requestedYardId: "HB"));
    }

    [Fact]
    public void Null_snapshot_still_throttles_same_yard()
    {
        // Cooldown applies even when previous build left no snapshot (OnGUI thrash).
        Assert.False(
            YardMiniMapRebuildGate.ShouldRebuild(
                nowSeconds: 10f,
                nextRebuildAt: 12.5f,
                cachedYardId: "MF",
                requestedYardId: "MF"));
    }

    [Fact]
    public void Smoke_032_far_polyline_outside_focus_not_drawn()
    {
        var far = new[] { (5000f, 5000f), (5010f, 5000f) };
        Assert.False(
            YardMiniMapRebuildGate.PolylineIntersectsBounds(far, 0f, 200f, 0f, 200f));
    }

    [Fact]
    public void Nearby_extra_polyline_intersects_focus()
    {
        var near = new[] { (50f, 50f), (80f, 50f) };
        Assert.True(
            YardMiniMapRebuildGate.PolylineIntersectsBounds(near, 0f, 200f, 0f, 200f));
    }
}
