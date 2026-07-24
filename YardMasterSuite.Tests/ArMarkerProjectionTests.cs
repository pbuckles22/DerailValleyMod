using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArMarkerProjectionTests
{
    private const float W = 800f;
    private const float H = 600f;
    private const float Margin = 28f;

    [Fact]
    public void ClampToScreen_keeps_insets()
    {
        Assert.True(ArMarkerProjection.ClampToScreen(-10f, 500f, W, H, 20f, out var x, out var y));
        Assert.Equal(20f, x);
        Assert.Equal(500f, y);

        Assert.False(ArMarkerProjection.ClampToScreen(400f, 300f, W, H, 20f, out x, out y));
        Assert.Equal(400f, x);
        Assert.Equal(300f, y);
    }

    [Fact]
    public void ApplyBehindCameraEdge_noop_when_ahead()
    {
        float x = 100f;
        float y = 200f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: false,
            viewRight: 5f,
            viewUp: 1f,
            W,
            H,
            Margin,
            ref x,
            ref y);
        Assert.Equal(100f, x);
        Assert.Equal(200f, y);
    }

    [Fact]
    public void ApplyBehindCameraEdge_right_lateral_hits_right_edge_not_center()
    {
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: true,
            viewRight: 10f,
            viewUp: 0.1f,
            W,
            H,
            Margin,
            ref x,
            ref y);

        Assert.InRange(x, W - Margin - 1f, W - Margin + 1f);
        Assert.True(Math.Abs(x - W * 0.5f) > 50f, "must not stay near horizontal center");
    }

    [Fact]
    public void ApplyBehindCameraEdge_left_lateral_hits_left_edge_not_center()
    {
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: true,
            viewRight: -10f,
            viewUp: 0.1f,
            W,
            H,
            Margin,
            ref x,
            ref y);

        Assert.InRange(x, Margin - 1f, Margin + 1f);
        Assert.True(Math.Abs(x - W * 0.5f) > 50f, "must not stay near horizontal center");
    }

    [Fact]
    public void ApplyBehindCameraEdge_strong_up_hits_top_edge()
    {
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: true,
            viewRight: 0.1f,
            viewUp: 10f,
            W,
            H,
            Margin,
            ref x,
            ref y);

        Assert.InRange(y, H - Margin - 1f, H - Margin + 1f);
    }

    [Fact]
    public void ApplyBehindCameraEdge_near_zero_lateral_still_on_edge()
    {
        // Degenerate behind-and-aligned: still must leave the fake center.
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: true,
            viewRight: 0f,
            viewUp: 0f,
            W,
            H,
            Margin,
            ref x,
            ref y);

        var onLeft = Math.Abs(x - Margin) < 1f;
        var onRight = Math.Abs(x - (W - Margin)) < 1f;
        var onBottom = Math.Abs(y - Margin) < 1f;
        var onTop = Math.Abs(y - (H - Margin)) < 1f;
        Assert.True(onLeft || onRight || onBottom || onTop, "must clamp to a screen edge");
        Assert.False(
            Math.Abs(x - W * 0.5f) < 1f && Math.Abs(y - H * 0.5f) < 1f,
            "must not remain at screen center");
    }

    [Fact]
    public void IsBehindCamera_uses_forward_dot()
    {
        Assert.True(ArMarkerProjection.IsBehindCamera(0.05f));
        Assert.True(ArMarkerProjection.IsBehindCamera(-1f));
        Assert.False(ArMarkerProjection.IsBehindCamera(0.06f));
        Assert.False(ArMarkerProjection.IsBehindCamera(2f));
    }

    [Fact]
    public void ToGuiY_flips_origin()
    {
        Assert.Equal(100f, ArMarkerProjection.ToGuiY(500f, 600f));
    }
}

public class ArMarkerDisplayTests
{
    [Fact]
    public void FormatLabel_glyph_and_distance()
    {
        Assert.Equal("▲ 120m", ArMarkerDisplay.FormatLabel(ArWaypointKind.Loco, 120.4f));
        Assert.Equal("⌂ 15m", ArMarkerDisplay.FormatLabel(ArWaypointKind.Station, 15f));
        Assert.Equal("● 3m", ArMarkerDisplay.FormatLabel(ArWaypointKind.Pin, 2.6f));
        Assert.Equal("▲", ArMarkerDisplay.FormatLabel(ArWaypointKind.Loco, null));
        Assert.Equal("120m", ArMarkerDisplay.FormatDistanceOnly(120.4f));
        Assert.Equal("", ArMarkerDisplay.FormatDistanceOnly(null));
    }
}

public class Tier2ArWaypointDebugTests
{
    [Fact]
    public void NextLogMessage_tracks_set_changes()
    {
        var none = new ArWaypointDebugSnapshot(false, false, false);
        Assert.Equal("T2 ar init: — AR", Tier2ArWaypointDebug.NextLogMessage(null, none));

        var pin = new ArWaypointDebugSnapshot(false, false, true);
        Assert.Equal("T2 ar change: pin", Tier2ArWaypointDebug.NextLogMessage(none, pin));

        var both = new ArWaypointDebugSnapshot(true, true, true);
        Assert.Equal("T2 ar change: loco+office+pin", Tier2ArWaypointDebug.NextLogMessage(pin, both));
        Assert.Null(Tier2ArWaypointDebug.NextLogMessage(both, both));
    }
}
