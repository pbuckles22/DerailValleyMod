using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BonusTimeDisplayTests
{
    [Fact]
    public void RemainingSeconds_null_when_no_limit()
    {
        Assert.Null(BonusTimeDisplay.RemainingSeconds(null, 10f));
        Assert.Null(BonusTimeDisplay.RemainingSeconds(0f, 10f));
        Assert.Null(BonusTimeDisplay.RemainingSeconds(-1f, 10f));
    }

    [Fact]
    public void RemainingSeconds_subtracts_elapsed()
    {
        Assert.Equal(50f, BonusTimeDisplay.RemainingSeconds(100f, 50f));
        Assert.Equal(100f, BonusTimeDisplay.RemainingSeconds(100f, null));
    }

    [Fact]
    public void Format_clock_and_placeholder()
    {
        Assert.Equal("— Bonus", BonusTimeDisplay.Format(null));
        Assert.Equal("Bonus 0:00", BonusTimeDisplay.Format(0f));
        Assert.Equal("Bonus 1:05", BonusTimeDisplay.Format(65f));
        Assert.Equal("Bonus 1:02:03", BonusTimeDisplay.Format(3723f));
    }

    [Fact]
    public void Format_rich_warn_critical()
    {
        Assert.Contains(BonusTimeDisplay.WarningColor, BonusTimeDisplay.Format(120f, richText: true));
        Assert.Contains(BonusTimeDisplay.CriticalColor, BonusTimeDisplay.Format(30f, richText: true));
        Assert.Equal("Bonus 10:00", BonusTimeDisplay.Format(600f, richText: true));
    }
}

public class ZoneEdgeDisplayTests
{
    [Fact]
    public void MetersRemaining_and_radius_helpers()
    {
        Assert.Equal(100f, ZoneEdgeDisplay.RadiusFromSqr(10_000f));
        Assert.Equal(30f, ZoneEdgeDisplay.DistanceFromSqr(900f));
        Assert.Equal(70f, ZoneEdgeDisplay.MetersRemaining(30f, 100f));
        Assert.Equal(-10f, ZoneEdgeDisplay.MetersRemaining(110f, 100f));
        Assert.Null(ZoneEdgeDisplay.MetersRemaining(null, 100f));
    }

    [Fact]
    public void Format_in_out_and_colors()
    {
        Assert.Equal("— Zone", ZoneEdgeDisplay.Format(null));
        Assert.Equal("Zone OUT", ZoneEdgeDisplay.Format(-1f));
        Assert.Equal("Zone 450m", ZoneEdgeDisplay.Format(450.4f));
        Assert.Contains(ZoneEdgeDisplay.WarningColor, ZoneEdgeDisplay.Format(100f, richText: true));
        Assert.Contains(ZoneEdgeDisplay.CriticalColor, ZoneEdgeDisplay.Format(10f, richText: true));
        Assert.Contains(ZoneEdgeDisplay.CriticalColor, ZoneEdgeDisplay.Format(-1f, richText: true));
    }
}

public class ActiveJobHudLineTests
{
    [Fact]
    public void Format_joins_chips()
    {
        Assert.Equal(
            "Job SM-FH-12  |  Bonus 14:32  |  Zone 820m",
            ActiveJobHudLine.Format("Job SM-FH-12", "Bonus 14:32", "Zone 820m"));
    }

    [Fact]
    public void FormatJobId_extra_count()
    {
        Assert.Equal("— Job", ActiveJobHudLine.FormatJobId(null, 0));
        Assert.Equal("Job SM-FH-12", ActiveJobHudLine.FormatJobId("SM-FH-12", 0));
        Assert.Equal("Job SM-FH-12 (+2)", ActiveJobHudLine.FormatJobId("SM-FH-12", 2));
    }
}

public class Tier2ActiveJobDebugTests
{
    [Fact]
    public void NextLogMessage_quiet_on_same_minute()
    {
        var a = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 14:32", "Zone 820m");
        var b = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 14:10", "Zone 820m");
        Assert.Null(Tier2ActiveJobDebug.NextLogMessage(a, b));

        var c = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 13:59", "Zone 820m");
        Assert.Equal(
            "T2 job change: Job SM-FH-12  |  Bonus 13:59  |  Zone 820m",
            Tier2ActiveJobDebug.NextLogMessage(a, c));
    }

    [Fact]
    public void NextLogMessage_appear_hide()
    {
        var hidden = new ActiveJobDebugSnapshot(false, null, null, null);
        var shown = new ActiveJobDebugSnapshot(true, "A", "Bonus 1:00", "Zone 10m");
        Assert.Equal("T2 job init (hidden)", Tier2ActiveJobDebug.NextLogMessage(null, hidden));
        Assert.Equal(
            "T2 job appear: Job A  |  Bonus 1:00  |  Zone 10m",
            Tier2ActiveJobDebug.NextLogMessage(hidden, shown));
        Assert.Equal("T2 job hide", Tier2ActiveJobDebug.NextLogMessage(shown, hidden));
    }
}
