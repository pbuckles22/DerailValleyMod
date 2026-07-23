using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2HeadingDebugTests
{
    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2HeadingDebug.NextLogMessage(null, new HeadingDebugSnapshot("NE"));
        Assert.Equal("T2 heading init: Heading NE", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_point_unchanged()
    {
        var snap = new HeadingDebugSnapshot("ENE");
        Assert.Null(Tier2HeadingDebug.NextLogMessage(snap, snap));
    }

    [Fact]
    public void NextLogMessage_logs_when_point_changes()
    {
        var msg = Tier2HeadingDebug.NextLogMessage(
            new HeadingDebugSnapshot("N"),
            new HeadingDebugSnapshot("NNE"));
        Assert.Equal("T2 heading change: Heading NNE", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_becoming_unknown()
    {
        var msg = Tier2HeadingDebug.NextLogMessage(
            new HeadingDebugSnapshot("E"),
            new HeadingDebugSnapshot(null));
        Assert.Equal("T2 heading change: — Heading", msg);
    }
}
