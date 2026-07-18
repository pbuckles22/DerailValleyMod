using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2LocalCarDebugTests
{
    private static LocalCarDebugSnapshot Hidden() =>
        new(
            visible: false,
            pipe: "— Pipe",
            handbrake: "— Handbrake",
            coupling: "— Couplers",
            carNumber: "Car XX",
            job: "— Job");

    private static LocalCarDebugSnapshot Visible(
        string pipe = "Pipe 2.0 bar",
        string handbrake = "Handbrake 1",
        string coupling = "Couplers F+ R-",
        string carNumber = "Car 3",
        string job = "Job FH-12") =>
        new(visible: true, pipe, handbrake, coupling, carNumber, job);

    [Fact]
    public void NextLogMessage_logs_init_when_first_visible()
    {
        var msg = Tier2LocalCarDebug.NextLogMessage(null, Visible());
        Assert.Equal(
            "T2 local-car init: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12",
            msg);
    }

    [Fact]
    public void NextLogMessage_logs_appear_and_hide()
    {
        Assert.Equal(
            "T2 local-car appear: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12",
            Tier2LocalCarDebug.NextLogMessage(Hidden(), Visible()));
        Assert.Equal(
            "T2 local-car hide",
            Tier2LocalCarDebug.NextLogMessage(Visible(), Hidden()));
    }

    [Fact]
    public void NextLogMessage_logs_change_while_visible()
    {
        var before = Visible(coupling: "Couplers F+ R-");
        var after = Visible(coupling: "Couplers F+ R+");
        var msg = Tier2LocalCarDebug.NextLogMessage(before, after);
        Assert.Equal(
            "T2 local-car change: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R+  |  Car 3  |  Job FH-12",
            msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Visible();
        Assert.Null(Tier2LocalCarDebug.NextLogMessage(snap, snap));
    }
}
