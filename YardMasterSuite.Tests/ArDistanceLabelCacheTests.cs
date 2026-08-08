using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 2026-08-07 smoke: rhythmic ~2.5 s hitch that vanishes when the mod is disabled in UMM.
/// AR captions redraw every frame, so they may only allocate when the shown meters change.
/// </summary>
public class ArDistanceLabelCacheTests
{
    [Fact]
    public void Smoke_hitch_cadence_subwmeter_drift_does_not_rebuild_caption()
    {
        var meters = ArDistanceLabelCache.RoundMeters(145.2f);
        Assert.Equal(145, meters);
        Assert.False(ArDistanceLabelCache.NeedsRebuild("145m", meters, ArDistanceLabelCache.RoundMeters(145.4f)));
    }

    [Fact]
    public void Crossing_a_whole_meter_rebuilds_caption()
    {
        Assert.True(ArDistanceLabelCache.NeedsRebuild("145m", 145, ArDistanceLabelCache.RoundMeters(146.1f)));
        Assert.True(ArDistanceLabelCache.NeedsRebuild(null, 145, 145));
    }

    [Fact]
    public void Unknown_or_negative_distance_rounds_to_zero()
    {
        Assert.Equal(0, ArDistanceLabelCache.RoundMeters(null));
        Assert.Equal(0, ArDistanceLabelCache.RoundMeters(-3f));
        Assert.Equal(0, ArDistanceLabelCache.RoundMeters(float.NaN));
    }

    [Fact]
    public void Radar_slot_reused_by_another_loco_rebuilds_even_at_same_distance()
    {
        Assert.True(ArDistanceLabelCache.NeedsRebuild("DE2 300m", 300, 300, "DE2", "DE6"));
        Assert.False(ArDistanceLabelCache.NeedsRebuild("DE2 300m", 300, 300, "DE2", "DE2"));
    }
}
