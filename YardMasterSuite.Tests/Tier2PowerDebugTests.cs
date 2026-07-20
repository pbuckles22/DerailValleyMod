using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2PowerDebugTests
{
    private static PowerDebugSnapshot NullTrain() =>
        new(hasLoco: false, load: "— Load");

    private static PowerDebugSnapshot Live(string load = "Load 42 %") =>
        new(hasLoco: true, load);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2PowerDebug.NextLogMessage(null, NullTrain());
        Assert.Equal("T2 power init (no-loco): — Load", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_gaining_loco()
    {
        var msg = Tier2PowerDebug.NextLogMessage(NullTrain(), Live());
        Assert.Equal("T2 power loco: Load 42 %", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_load_changes()
    {
        var msg = Tier2PowerDebug.NextLogMessage(Live("Load 42 %"), Live("Load 81 %"));
        Assert.Equal("T2 power change: Load 81 %", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Live();
        Assert.Null(Tier2PowerDebug.NextLogMessage(snap, snap));
    }
}
