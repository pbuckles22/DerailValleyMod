using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke regressions for sticky yard / MFMB office fence (formerly 4.13 mini-map; stick still used by desk + FILO).
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
}
