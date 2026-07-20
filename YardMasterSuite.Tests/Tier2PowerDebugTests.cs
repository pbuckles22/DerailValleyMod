using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2PowerDebugTests
{
    private static PowerDebugSnapshot NullTrain() =>
        new(hasLoco: false, load: "— Load", motors: "— Motors");

    private static PowerDebugSnapshot Live(
        string load = "Load 42 %",
        string motors = "Motors OK") =>
        new(hasLoco: true, load, motors);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2PowerDebug.NextLogMessage(null, NullTrain());
        Assert.Equal("T2 power init (no-loco): — Load  |  — Motors", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_gaining_loco()
    {
        var msg = Tier2PowerDebug.NextLogMessage(NullTrain(), Live());
        Assert.Equal("T2 power loco: Load 42 %  |  Motors OK", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_load_or_motors_change()
    {
        var msg = Tier2PowerDebug.NextLogMessage(Live("Load 42 %"), Live("Load 81 %"));
        Assert.Equal("T2 power change: Load 81 %  |  Motors OK", msg);

        msg = Tier2PowerDebug.NextLogMessage(Live(motors: "Motors OK"), Live(motors: "Motors Hot"));
        Assert.Equal("T2 power change: Load 42 %  |  Motors Hot", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Live();
        Assert.Null(Tier2PowerDebug.NextLogMessage(snap, snap));
    }
}
