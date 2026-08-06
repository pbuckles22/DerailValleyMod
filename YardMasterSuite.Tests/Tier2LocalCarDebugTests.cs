using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2LocalCarDebugTests
{
    private static LocalCarDebugSnapshot Hidden() =>
        new(
            visible: false,
            handbrake: "— Handbrake",
            coupling: "— Couplers",
            job: null,
            track: null);

    private static LocalCarDebugSnapshot Visible(
        string handbrake = "Handbrake 1",
        string coupling = "Couplers F+ R-",
        string job = "Job FH-12",
        string track = "Track SM-O6I",
        string? identity = "46t") =>
        new(visible: true, handbrake, coupling, job, track, identity);

    [Fact]
    public void NextLogMessage_logs_init_when_first_visible()
    {
        var msg = Tier2LocalCarDebug.NextLogMessage(null, Visible());
        Assert.Equal(
            "T2 local-car init: Handbrake 1  |  Couplers F+ R-  |  Job FH-12  |  Track SM-O6I  |  46t",
            msg);
    }

    [Fact]
    public void NextLogMessage_logs_appear_and_hide()
    {
        Assert.Equal(
            "T2 local-car appear: Handbrake 1  |  Couplers F+ R-  |  Job FH-12  |  Track SM-O6I  |  46t",
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
            "T2 local-car change: Handbrake 1  |  Couplers F+ R+  |  Job FH-12  |  Track SM-O6I  |  46t",
            msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Visible();
        Assert.Null(Tier2LocalCarDebug.NextLogMessage(snap, snap));
    }
}
