using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2ParkDebugTests
{
    [Fact]
    public void NextLogMessage_init_set()
    {
        var msg = Tier2ParkDebug.NextLogMessage(null, new ParkDebugSnapshot(true, "NE"));
        Assert.Equal("T2 mark init: Marked NE", msg);
    }

    [Fact]
    public void NextLogMessage_same_point_is_null()
    {
        var snap = new ParkDebugSnapshot(true, "NE");
        Assert.Null(Tier2ParkDebug.NextLogMessage(snap, snap));
    }

    [Fact]
    public void NextLogMessage_cleared()
    {
        var msg = Tier2ParkDebug.NextLogMessage(
            new ParkDebugSnapshot(true, "NE"),
            new ParkDebugSnapshot(false, null));
        Assert.Equal("T2 mark change: — Marked", msg);
    }

    [Fact]
    public void NextLogMessage_bearing_change()
    {
        var msg = Tier2ParkDebug.NextLogMessage(
            new ParkDebugSnapshot(true, "NE"),
            new ParkDebugSnapshot(true, "E"));
        Assert.Equal("T2 mark change: Marked E", msg);
    }

    [Fact]
    public void NextLogMessage_here()
    {
        var msg = Tier2ParkDebug.NextLogMessage(
            new ParkDebugSnapshot(true, "N"),
            new ParkDebugSnapshot(true, "here"));
        Assert.Equal("T2 mark change: Marked here", msg);
    }
}
