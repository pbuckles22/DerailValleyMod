using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2CouplerDebugTests
{
    private static CouplerDebugSnapshot Hidden() =>
        new(visible: false, coupling: "— Couplers");

    private static CouplerDebugSnapshot Visible(string coupling = "Couplers F* R-") =>
        new(visible: true, coupling: coupling);

    [Fact]
    public void NextLogMessage_logs_init_hidden_when_no_target()
    {
        var msg = Tier2CouplerDebug.NextLogMessage(null, Hidden());
        Assert.Equal("T2 coupler init (hidden)", msg);
    }

    [Fact]
    public void NextLogMessage_logs_init_when_first_visible()
    {
        var msg = Tier2CouplerDebug.NextLogMessage(null, Visible());
        Assert.Equal("T2 coupler init: Couplers F* R-", msg);
    }

    [Fact]
    public void NextLogMessage_logs_appear_and_hide()
    {
        Assert.Equal(
            "T2 coupler appear: Couplers F* R-",
            Tier2CouplerDebug.NextLogMessage(Hidden(), Visible()));
        Assert.Equal(
            "T2 coupler hide",
            Tier2CouplerDebug.NextLogMessage(Visible(), Hidden()));
    }

    [Fact]
    public void NextLogMessage_logs_change_when_tight_or_loose_changes()
    {
        var loose = Visible("Couplers F* R-");
        var tight = Visible("Couplers F+ R-");
        Assert.Equal(
            "T2 coupler change: Couplers F+ R-",
            Tier2CouplerDebug.NextLogMessage(loose, tight));
    }

    [Fact]
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Visible();
        Assert.Null(Tier2CouplerDebug.NextLogMessage(snap, snap));
    }
}
