using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2ConsistDebugTests
{
    private static ConsistDebugSnapshot NullTrain() =>
        new(hasLoco: false, cars: "— Cars", handbrakes: "— Handbrakes");

    private static ConsistDebugSnapshot Live(
        string cars = "Cars 8",
        string handbrakes = "Handbrakes 3") =>
        new(hasLoco: true, cars, handbrakes);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2ConsistDebug.NextLogMessage(null, NullTrain());
        Assert.Equal("T2 consist init (no-loco): — Cars  |  — Handbrakes", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_gaining_loco()
    {
        var msg = Tier2ConsistDebug.NextLogMessage(NullTrain(), Live());
        Assert.Equal("T2 consist loco: Cars 8  |  Handbrakes 3", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_losing_loco()
    {
        var msg = Tier2ConsistDebug.NextLogMessage(Live(), NullTrain());
        Assert.Equal("T2 consist no-loco: — Cars  |  — Handbrakes", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_fields_change()
    {
        var before = Live(handbrakes: "Handbrakes 3");
        var after = Live(handbrakes: "Handbrakes 2");
        var msg = Tier2ConsistDebug.NextLogMessage(before, after);
        Assert.Equal("T2 consist change: Cars 8  |  Handbrakes 2", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Live();
        Assert.Null(Tier2ConsistDebug.NextLogMessage(snap, snap));
    }
}
