using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2PowerDebugTests
{
    private static PowerDebugSnapshot NullTrain() =>
        new(hasLoco: false, load: "— Load", motors: "— Motors", fuel: "— Fuel", oil: "— Oil");

    private static PowerDebugSnapshot Live(
        string load = "Load 42 %",
        string motors = "Motors OK",
        string fuel = "Fuel 67 %",
        string oil = "Oil 55 %") =>
        new(hasLoco: true, load, motors, fuel, oil);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2PowerDebug.NextLogMessage(null, NullTrain());
        Assert.Equal("T2 power init (no-loco): — Load  |  — Motors  |  — Fuel  |  — Oil", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_gaining_loco()
    {
        var msg = Tier2PowerDebug.NextLogMessage(NullTrain(), Live());
        Assert.Equal("T2 power loco: Load 42 %  |  Motors OK  |  Fuel 67 %  |  Oil 55 %", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_load_motors_or_fluids_change()
    {
        var msg = Tier2PowerDebug.NextLogMessage(Live("Load 42 %"), Live("Load 81 %"));
        Assert.Equal("T2 power change: Load 81 %  |  Motors OK  |  Fuel 67 %  |  Oil 55 %", msg);

        msg = Tier2PowerDebug.NextLogMessage(Live(motors: "Motors OK"), Live(motors: "Motors Hot"));
        Assert.Equal("T2 power change: Load 42 %  |  Motors Hot  |  Fuel 67 %  |  Oil 55 %", msg);

        msg = Tier2PowerDebug.NextLogMessage(Live(fuel: "Fuel 67 %"), Live(fuel: "Fuel 15 %"));
        Assert.Equal("T2 power change: Load 42 %  |  Motors OK  |  Fuel 15 %  |  Oil 55 %", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Live();
        Assert.Null(Tier2PowerDebug.NextLogMessage(snap, snap));
    }
}
