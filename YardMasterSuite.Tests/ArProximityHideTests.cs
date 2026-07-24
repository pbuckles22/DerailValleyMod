using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArProximityHideTests
{
    [Fact]
    public void ShouldHideLocoMarker_when_player_on_that_loco()
    {
        Assert.True(ArProximityHide.ShouldHideLocoMarker(true));
        Assert.False(ArProximityHide.ShouldHideLocoMarker(false));
    }

    [Fact]
    public void ShouldHideStationMarker_uses_xz_footprint_ignores_y()
    {
        var box = new Aabb3(0f, 10f, 0f, 10f, 11f, 8f); // thin Y slab high up
        Assert.True(ArProximityHide.ShouldHideStationMarker(box, 5f, 4f)); // player Y irrelevant
        Assert.False(ArProximityHide.ShouldHideStationMarker(box, 11f, 4f));
    }

    [Fact]
    public void Aabb3_InflateXZ_shrinks_footprint_only()
    {
        var box = new Aabb3(0f, 0f, 0f, 10f, 4f, 10f);
        var shrunk = box.InflateXZ(-2f);
        Assert.Equal(1f, shrunk.MinX, 3);
        Assert.Equal(9f, shrunk.MaxX, 3);
        Assert.Equal(0f, shrunk.MinY, 3);
        Assert.Equal(4f, shrunk.MaxY, 3);
        Assert.False(shrunk.ContainsXZ(0.5f, 5f));
        Assert.True(shrunk.ContainsXZ(5f, 5f));
    }

    [Fact]
    public void Aabb3_FromCenterExtents_builds_symmetric_box()
    {
        var box = Aabb3.FromCenterExtents(100f, 10f, 200f, 4f, 3f, 5f);
        Assert.True(box.ContainsXZ(100f, 200f));
        Assert.False(box.ContainsXZ(105f, 200f));
    }

    /// <summary>
    /// Lobby-box geometry (A.4): half-extent 8 + shrink −2 keeps ~14 m door outside hide.
    /// </summary>
    [Fact]
    public void Lobby_fallback_geometry_keeps_door_at_14m_outside()
    {
        var box = Aabb3.FromCenterExtents(0f, 1f, 0f, 8f, 6f, 8f).InflateXZ(-2f);
        Assert.True(ArProximityHide.ShouldHideStationMarker(box, 0f, 0f));
        Assert.False(ArProximityHide.ShouldHideStationMarker(box, 14f, 0f));
        Assert.False(ArProximityHide.ShouldHideStationMarker(box, 8f, 0f)); // apron near door
    }
}
