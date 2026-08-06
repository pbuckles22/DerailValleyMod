using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class YardMiniMapProjectionTests
{
    [Fact]
    public void TryFitBounds_empty_fails()
    {
        Assert.False(YardMiniMapProjection.TryFitBounds(
            Array.Empty<(float X, float Z)>(),
            padding: 10f,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void TryFitBounds_pads_aabb()
    {
        var ok = YardMiniMapProjection.TryFitBounds(
            new[] { (0f, 0f), (100f, 50f) },
            padding: 10f,
            out var minX,
            out var maxX,
            out var minZ,
            out var maxZ);
        Assert.True(ok);
        Assert.Equal(-10f, minX);
        Assert.Equal(110f, maxX);
        Assert.Equal(-10f, minZ);
        Assert.Equal(60f, maxZ);
    }

    [Fact]
    public void TryWorldToPanel_maps_corners_north_up()
    {
        // Bounds 0..100 X, 0..100 Z. Panel 200×200 at (10,20).
        // SW (0,0) → bottom-left; NE (100,100) → top-right.
        Assert.True(YardMiniMapProjection.TryWorldToPanel(
            0f, 0f, 0f, 100f, 0f, 100f, 10f, 20f, 200f, 200f,
            out var swX, out var swY));
        Assert.Equal(10f, swX, 3);
        Assert.Equal(220f, swY, 3);

        Assert.True(YardMiniMapProjection.TryWorldToPanel(
            100f, 100f, 0f, 100f, 0f, 100f, 10f, 20f, 200f, 200f,
            out var neX, out var neY));
        Assert.Equal(210f, neX, 3);
        Assert.Equal(20f, neY, 3);
    }

    [Fact]
    public void TryWorldToPanel_center()
    {
        Assert.True(YardMiniMapProjection.TryWorldToPanel(
            50f, 50f, 0f, 100f, 0f, 100f, 0f, 0f, 100f, 100f,
            out var x, out var y));
        Assert.Equal(50f, x, 3);
        Assert.Equal(50f, y, 3);
    }

    [Fact]
    public void TryWorldToPanel_degenerate_bounds_fails()
    {
        Assert.False(YardMiniMapProjection.TryWorldToPanel(
            0f, 0f, 5f, 5f, 0f, 100f, 0f, 0f, 100f, 100f,
            out _, out _));
    }

    [Fact]
    public void HeadingTickOffset_north_points_up_on_panel()
    {
        YardMiniMapProjection.HeadingTickOffset(0f, 10f, out var dx, out var dy);
        Assert.Equal(0f, dx, 3);
        Assert.True(dy < 0f); // OnGUI Y grows down → north = negative dy
        Assert.Equal(-10f, dy, 3);
    }

    [Fact]
    public void HeadingTickOffset_east_points_right()
    {
        YardMiniMapProjection.HeadingTickOffset(90f, 10f, out var dx, out var dy);
        Assert.Equal(10f, dx, 3);
        Assert.Equal(0f, dy, 3);
    }

    [Fact]
    public void Landmark_same_as_world_to_panel()
    {
        Assert.True(YardMiniMapProjection.TryWorldToPanel(
            25f, 75f, 0f, 100f, 0f, 100f, 0f, 0f, 200f, 100f,
            out var x, out var y));
        Assert.Equal(50f, x, 3);
        Assert.Equal(25f, y, 3);
    }

    [Fact]
    public void ClampToPanel_pulls_outside_point_to_edge()
    {
        var x = -50f;
        var y = 500f;
        YardMiniMapProjection.ClampToPanel(10f, 20f, 100f, 100f, insetPixels: 4f, ref x, ref y);
        Assert.Equal(14f, x, 3);
        Assert.Equal(116f, y, 3);
    }

    [Fact]
    public void IsOutsideBounds_detects_exterior()
    {
        Assert.False(YardMiniMapProjection.IsOutsideBounds(50f, 50f, 0f, 100f, 0f, 100f));
        Assert.True(YardMiniMapProjection.IsOutsideBounds(150f, 50f, 0f, 100f, 0f, 100f));
    }

    [Fact]
    public void TryOffMapEdge_inside_returns_false()
    {
        Assert.False(YardMiniMapProjection.TryOffMapEdge(
            50f, 50f, 0f, 100f, 0f, 100f, 0f, 0f, 100f, 100f, 4f,
            out _, out _, out _, out _));
    }

    [Fact]
    public void TryOffMapEdge_north_of_map_places_top_edge()
    {
        Assert.True(YardMiniMapProjection.TryOffMapEdge(
            50f, 200f, 0f, 100f, 0f, 100f, 0f, 0f, 100f, 100f, 4f,
            out var ex, out var ey, out var dx, out var dy));
        Assert.Equal(50f, ex, 3);
        Assert.Equal(4f, ey, 3); // top inset
        Assert.True(dy < 0f); // arrow points up (off-map north)
        Assert.True(Math.Abs(dx) < 0.2f);
    }
}
