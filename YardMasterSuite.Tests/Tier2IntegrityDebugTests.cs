using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2IntegrityDebugTests
{
    private static IntegrityDebugSnapshot Foot() =>
        new(onCar: false, pipe: "— Pipe", handbrake: "— Handbrake", coupling: "— Couplers");

    private static IntegrityDebugSnapshot OnCar(
        string pipe = "Pipe 2.0 bar",
        string handbrake = "Handbrake 1",
        string coupling = "Couplers F- R+") =>
        new(onCar: true, pipe, handbrake, coupling);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2IntegrityDebug.NextLogMessage(null, Foot());
        Assert.Equal("T2 integrity init (on-foot): — Pipe  |  — Handbrake  |  — Couplers", msg);
    }

    [Fact]
    public void NextLogMessage_logs_on_car_when_mounting()
    {
        var msg = Tier2IntegrityDebug.NextLogMessage(Foot(), OnCar());
        Assert.Equal("T2 integrity on-car: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F- R+", msg);
    }

    [Fact]
    public void NextLogMessage_logs_on_foot_when_dismounting()
    {
        var msg = Tier2IntegrityDebug.NextLogMessage(OnCar(), Foot());
        Assert.Equal("T2 integrity on-foot: — Pipe  |  — Handbrake  |  — Couplers", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_integrity_fields_change_while_on_car()
    {
        var before = OnCar(coupling: "Couplers F- R+");
        var after = OnCar(coupling: "Couplers F- R-");
        var msg = Tier2IntegrityDebug.NextLogMessage(before, after);
        Assert.Equal("T2 integrity change: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F- R-", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = OnCar();
        Assert.Null(Tier2IntegrityDebug.NextLogMessage(snap, snap));
    }
}
