using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class YardMiniMapYardStickTests
{
    [Fact]
    public void Resolve_keeps_sticky_when_nearest_flips_to_satellite()
    {
        var yards = new[] { "MFMB", "MF" };
        Assert.Equal("MF", YardMiniMapYardStick.Resolve("MF", yards, nearestYardId: "MFMB"));
    }

    [Fact]
    public void Resolve_ignores_satellite_job_zone_alone()
    {
        Assert.Null(YardMiniMapYardStick.Resolve("MF", new[] { "MFMB" }, nearestYardId: "MFMB"));
    }

    [Fact]
    public void Resolve_satellite_fence_requires_nearest_satellite()
    {
        Assert.Equal(
            "MF",
            YardMiniMapYardStick.Resolve(
                "MF",
                new[] { "MFMB", "MF" },
                nearestYardId: "MF",
                insideFenceSatellites: new[] { "MFMB" }));
        Assert.Equal(
            "MFMB",
            YardMiniMapYardStick.Resolve(
                "MF",
                new[] { "MFMB", "MF" },
                nearestYardId: "MFMB",
                insideFenceSatellites: new[] { "MFMB" }));
    }

    [Fact]
    public void Resolve_empty_zones_null()
    {
        Assert.Null(YardMiniMapYardStick.Resolve("MF", Array.Empty<string>(), "MF"));
    }

    [Fact]
    public void IsSatelliteYard_mfmb()
    {
        Assert.True(YardMiniMapYardStick.IsSatelliteYard("MFMB"));
        Assert.False(YardMiniMapYardStick.IsSatelliteYard("MF"));
    }
}
