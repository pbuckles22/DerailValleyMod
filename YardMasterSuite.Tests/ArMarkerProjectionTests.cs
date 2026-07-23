using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArMarkerProjectionTests
{
    [Fact]
    public void ClampToScreen_keeps_insets()
    {
        Assert.True(ArMarkerProjection.ClampToScreen(-10f, 500f, 800f, 600f, 20f, out var x, out var y));
        Assert.Equal(20f, x);
        Assert.Equal(500f, y);

        Assert.False(ArMarkerProjection.ClampToScreen(400f, 300f, 800f, 600f, 20f, out x, out y));
        Assert.Equal(400f, x);
        Assert.Equal(300f, y);
    }

    [Fact]
    public void ApplyBehindCameraFlip_mirrors_across_center()
    {
        float x = 100f;
        float y = 200f;
        ArMarkerProjection.ApplyBehindCameraFlip(true, 800f, 600f, ref x, ref y);
        Assert.Equal(700f, x);
        Assert.Equal(400f, y);

        x = 100f;
        y = 200f;
        ArMarkerProjection.ApplyBehindCameraFlip(false, 800f, 600f, ref x, ref y);
        Assert.Equal(100f, x);
        Assert.Equal(200f, y);
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
