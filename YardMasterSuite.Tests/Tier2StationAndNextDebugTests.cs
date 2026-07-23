using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2StationWaypointDebugTests
{
    [Fact]
    public void NextLogMessage_logs_init_and_change()
    {
        var outOfZone = new StationWaypointDebugSnapshot(false, null, null);
        Assert.Equal("T2 station init: — Station", Tier2StationWaypointDebug.NextLogMessage(null, outOfZone));

        var inZone = new StationWaypointDebugSnapshot(true, "SM", "NE");
        Assert.Equal(
            "T2 station change: Station SM NE",
            Tier2StationWaypointDebug.NextLogMessage(outOfZone, inZone));
        Assert.Null(Tier2StationWaypointDebug.NextLogMessage(inZone, inZone));
    }
}

public class Tier2NextStationDebugTests
{
    [Fact]
    public void NextLogMessage_logs_init_and_change()
    {
        var hidden = new NextStationDebugSnapshot(false, null);
        Assert.Equal("T2 next-station init: — Next", Tier2NextStationDebug.NextLogMessage(null, hidden));

        var shown = new NextStationDebugSnapshot(true, "Next: SM [12.5 km]");
        Assert.Equal(
            "T2 next-station change: Next: SM [12.5 km]",
            Tier2NextStationDebug.NextLogMessage(hidden, shown));
        Assert.Null(Tier2NextStationDebug.NextLogMessage(shown, shown));
    }
}
