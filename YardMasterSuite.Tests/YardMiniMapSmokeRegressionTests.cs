using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke regressions from Player.log + screenshots (4.13).
/// 0.6.26 FAIL: Station MF→MFMB N flipped Yard to MFMB via track AABB — too early vs fences.
/// </summary>
public class YardMiniMapSmokeRegressionTests
{
    [Fact]
    public void Smoke_log_station_flips_to_mfmb_n_keeps_mf_outside_office_fence()
    {
        // Player.log: Station MF WSW → Station MFMB N → (was) yard MFMB
        Assert.Equal(
            "MF",
            YardMiniMapYardStick.Resolve(
                stickyYardId: "MF",
                inZoneYardIds: new[] { "MF", "MFMB" },
                nearestYardId: "MFMB",
                insideFenceSatellites: null));
    }

    [Fact]
    public void Smoke_station_mf_keeps_mf_even_if_stale_fence_list()
    {
        // Parent sticky + MF still in zone, empty fence after leaving compound.
        Assert.Equal(
            "MF",
            YardMiniMapYardStick.Resolve(
                stickyYardId: "MF",
                inZoneYardIds: new[] { "MF", "MFMB" },
                nearestYardId: "MF",
                insideFenceSatellites: null));
    }

    [Fact]
    public void Smoke_mfmb_job_zone_alone_outside_fence_hides_map()
    {
        Assert.Null(
            YardMiniMapYardStick.Resolve(
                stickyYardId: "MF",
                inZoneYardIds: new[] { "MFMB" },
                nearestYardId: "MFMB",
                insideFenceSatellites: null));
    }

    [Fact]
    public void Smoke_inside_mfmb_office_fence_shows_mfmb()
    {
        Assert.Equal(
            "MFMB",
            YardMiniMapYardStick.Resolve(
                stickyYardId: "MF",
                inZoneYardIds: new[] { "MF", "MFMB" },
                nearestYardId: "MFMB",
                insideFenceSatellites: new[] { "MFMB" }));
    }

    [Fact]
    public void Smoke_office_fence_radius_5m_temp()
    {
        Assert.True(YardMiniMapYardStick.IsInsideOfficeFence(0f, 0f, 0f, 4f, 5f));
        Assert.False(YardMiniMapYardStick.IsInsideOfficeFence(0f, 0f, 0f, 50f, 5f));
        Assert.Equal(5f, YardMiniMapYardStick.SatelliteFenceRadiusMeters);
    }

    /// <summary>0.6.27 FAIL: ~50 m out, Station MF, Yard MFMB — fence must not steal while nearest is MF.</summary>
    [Fact]
    public void Smoke_station_mf_not_stolen_by_near_mfmb_office_fence()
    {
        Assert.Equal(
            "MF",
            YardMiniMapYardStick.Resolve(
                stickyYardId: "MF",
                inZoneYardIds: new[] { "MF", "MFMB" },
                nearestYardId: "MF",
                insideFenceSatellites: new[] { "MFMB" }));
    }

    [Fact]
    public void Smoke_off_map_uses_edge_arrow_not_interior_pin()
    {
        Assert.True(YardMiniMapProjection.IsOutsideBounds(50f, -40f, 0f, 100f, 0f, 100f));
        Assert.True(YardMiniMapProjection.TryOffMapEdge(
            50f, -40f, 0f, 100f, 0f, 100f, 0f, 0f, 100f, 100f, 6f,
            out var ex, out var ey, out var dx, out var dy));
        Assert.Equal(50f, ex, 3);
        Assert.Equal(94f, ey, 3);
        Assert.True(dy > 0f);
        Assert.True(Math.Abs(dx) < 0.2f);
    }
}
