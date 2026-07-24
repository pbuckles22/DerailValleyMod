using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2LookAtDebugTests
{
    private static LocalCarDebugSnapshot Hidden() =>
        new(
            visible: false,
            pipe: "— Pipe",
            handbrake: "— Handbrake",
            coupling: "— Couplers",
            carNumber: "Car XX",
            job: null,
            track: null);

    private static LocalCarDebugSnapshot Visible(
        string pipe = "Pipe 2.0 bar",
        string handbrake = "Handbrake 1",
        string coupling = "Couplers F+ R-",
        string carNumber = "Car 3",
        string job = "Job FH-12",
        string track = "Track SM-O6I") =>
        new(visible: true, pipe, handbrake, coupling, carNumber, job, track);

    [Fact]
    public void NextLogMessage_logs_init_when_first_visible()
    {
        var msg = Tier2LookAtDebug.NextLogMessage(null, Visible());
        Assert.Equal(
            "T2 look-at init: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12  |  Track SM-O6I",
            msg);
    }

    [Fact]
    public void NextLogMessage_logs_appear_and_hide()
    {
        Assert.Equal(
            "T2 look-at appear: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12  |  Track SM-O6I",
            Tier2LookAtDebug.NextLogMessage(Hidden(), Visible()));
        Assert.Equal(
            "T2 look-at hide",
            Tier2LookAtDebug.NextLogMessage(Visible(), Hidden()));
    }

    [Fact]
    public void NextLogMessage_logs_change_while_visible()
    {
        var before = Visible(coupling: "Couplers F+ R-");
        var after = Visible(coupling: "Couplers F+ R+");
        var msg = Tier2LookAtDebug.NextLogMessage(before, after);
        Assert.Equal(
            "T2 look-at change: Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R+  |  Car 3  |  Job FH-12  |  Track SM-O6I",
            msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Visible();
        Assert.Null(Tier2LookAtDebug.NextLogMessage(snap, snap));
    }
}
