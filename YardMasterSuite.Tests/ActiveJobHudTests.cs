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

public class PreviewEdgeDisplayTests
{
    [Fact]
    public void MetersRemaining_and_radius_helpers()
    {
        Assert.Equal(100f, PreviewEdgeDisplay.RadiusFromSqr(10_000f));
        Assert.Equal(30f, PreviewEdgeDisplay.DistanceFromSqr(900f));
        // 100 − 30 player − 30 safety buffer = 40
        Assert.Equal(40f, PreviewEdgeDisplay.MetersRemaining(30f, 100f));
        // Past geometric edge → more negative after buffer
        Assert.Equal(-40f, PreviewEdgeDisplay.MetersRemaining(110f, 100f));
        Assert.Null(PreviewEdgeDisplay.MetersRemaining(null, 100f));
    }

    [Fact]
    public void MetersRemaining_applies_safety_buffer()
    {
        Assert.Equal(30f, PreviewEdgeDisplay.SafetyBufferMeters);
        // Cab 5 m inside geometric edge → buffer makes HUD show OUT territory
        Assert.Equal(-25f, PreviewEdgeDisplay.MetersRemaining(95f, 100f));
    }

    [Fact]
    public void Format_preview_in_out_and_colors()
    {
        Assert.Equal("— Preview", PreviewEdgeDisplay.Format(null));
        Assert.Equal("Preview OUT", PreviewEdgeDisplay.Format(-1f));
        Assert.Equal("Preview 450m", PreviewEdgeDisplay.Format(450.4f));
        Assert.Contains(PreviewEdgeDisplay.WarningColor, PreviewEdgeDisplay.Format(100f, richText: true));
        Assert.Contains(PreviewEdgeDisplay.CriticalColor, PreviewEdgeDisplay.Format(10f, richText: true));
        Assert.Contains(PreviewEdgeDisplay.CriticalColor, PreviewEdgeDisplay.Format(-1f, richText: true));
    }
}

public class ActiveJobHudLineTests
{
    [Fact]
    public void Format_taken_job_is_job_and_bonus_only()
    {
        Assert.Equal(
            "Job SM-FH-12  |  Bonus 14:32",
            ActiveJobHudLine.Format("Job SM-FH-12", "Bonus 14:32"));
    }

    [Fact]
    public void Format_preview_only_bar()
    {
        Assert.Equal("Preview 180m", ActiveJobHudLine.FormatPreview("Preview 180m"));
    }

    [Fact]
    public void FormatPrep_license_warn_alone_or_with_preview()
    {
        Assert.Null(ActiveJobHudLine.FormatPrep(null, null));
        Assert.Equal("No license: FH", ActiveJobHudLine.FormatPrep("No license: FH", null));
        Assert.Equal("Preview 180m", ActiveJobHudLine.FormatPrep(null, "Preview 180m"));
        Assert.Equal(
            "No license: FH  |  Preview 180m",
            ActiveJobHudLine.FormatPrep("No license: FH", "Preview 180m"));
    }

    [Fact]
    public void FormatCancelled_red_when_rich()
    {
        Assert.Equal("Job SM-FH-12  |  Cancelled", ActiveJobHudLine.FormatCancelled("SM-FH-12"));
        Assert.Contains(ActiveJobHudLine.CancelledColor, ActiveJobHudLine.FormatCancelled("SM-FH-12", richText: true));
        Assert.Equal("Cancelled", ActiveJobHudLine.FormatCancelled(null));
    }

    [Fact]
    public void FormatJobId_extra_count()
    {
        Assert.Equal("— Job", ActiveJobHudLine.FormatJobId(null, 0));
        Assert.Equal("Job SM-FH-12", ActiveJobHudLine.FormatJobId("SM-FH-12", 0));
        Assert.Equal("Job SM-FH-12 (+2)", ActiveJobHudLine.FormatJobId("SM-FH-12", 2));
    }

    [Fact]
    public void IsCancelledState_abandoned_or_expired_only()
    {
        Assert.True(ActiveJobHudLine.IsCancelledState("Abandoned"));
        Assert.True(ActiveJobHudLine.IsCancelledState("Expired"));
        Assert.False(ActiveJobHudLine.IsCancelledState("InProgress"));
        Assert.False(ActiveJobHudLine.IsCancelledState("Completed"));
        Assert.False(ActiveJobHudLine.IsCancelledState("Failed"));
        Assert.False(ActiveJobHudLine.IsCancelledState(null));
    }
}

public class LicenseWarnDisplayTests
{
    [Fact]
    public void Format_null_when_empty()
    {
        Assert.Null(LicenseWarnDisplay.Format(null));
        Assert.Null(LicenseWarnDisplay.Format(Array.Empty<string>()));
        Assert.Null(LicenseWarnDisplay.Format(new[] { "  ", "" }));
    }

    [Fact]
    public void Format_single_and_multiple_codes()
    {
        Assert.Equal("No license: FH", LicenseWarnDisplay.Format(new[] { "FH" }));
        Assert.Equal("No license: FH, HZ1", LicenseWarnDisplay.Format(new[] { "FH", "HZ1" }));
        Assert.Contains(LicenseWarnDisplay.WarnColor, LicenseWarnDisplay.Format(new[] { "FH" }, richText: true)!);
    }

    [Fact]
    public void Abbreviate_ticket_style_codes()
    {
        Assert.Equal("FH", LicenseWarnDisplay.Abbreviate("FreightHaul"));
        Assert.Equal("SH", LicenseWarnDisplay.Abbreviate("Shunting"));
        Assert.Equal("LH", LicenseWarnDisplay.Abbreviate("LogisticalHaul"));
        Assert.Equal("HZ1", LicenseWarnDisplay.Abbreviate("Hazmat1"));
        Assert.Equal("TL2", LicenseWarnDisplay.Abbreviate("TrainLength2"));
        Assert.Equal("FH", LicenseWarnDisplay.Abbreviate("FH"));
        Assert.Equal(string.Empty, LicenseWarnDisplay.Abbreviate(null));
    }

    [Fact]
    public void NormalizeCodes_dedupes_and_abbreviates()
    {
        var codes = LicenseWarnDisplay.NormalizeCodes(new[] { "FreightHaul", "FH", "Hazmat1", "  " });
        Assert.Equal(new[] { "FH", "HZ1" }, codes);
    }
}

public class Tier2ActiveJobDebugTests
{
    [Fact]
    public void NextLogMessage_quiet_on_same_minute()
    {
        var a = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 14:32", null);
        var b = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 14:10", null);
        Assert.Null(Tier2ActiveJobDebug.NextLogMessage(a, b));

        var c = new ActiveJobDebugSnapshot(true, "SM-FH-12", "Bonus 13:59", null);
        Assert.Equal(
            "T2 job change: Job SM-FH-12  |  Bonus 13:59",
            Tier2ActiveJobDebug.NextLogMessage(a, c));
    }

    [Fact]
    public void NextLogMessage_appear_hide_and_preview()
    {
        var hidden = new ActiveJobDebugSnapshot(false, null, null, null);
        var shown = new ActiveJobDebugSnapshot(true, "A", "Bonus 1:00", null);
        Assert.Equal("T2 job init (hidden)", Tier2ActiveJobDebug.NextLogMessage(null, hidden));
        Assert.Equal(
            "T2 job appear: Job A  |  Bonus 1:00",
            Tier2ActiveJobDebug.NextLogMessage(hidden, shown));
        Assert.Equal("T2 job hide", Tier2ActiveJobDebug.NextLogMessage(shown, hidden));

        var preview = new ActiveJobDebugSnapshot(true, null, null, "Preview 10m");
        Assert.Equal(
            "T2 job appear: Preview 10m",
            Tier2ActiveJobDebug.NextLogMessage(hidden, preview));

        var license = new ActiveJobDebugSnapshot(true, null, null, "Preview 10m", "No license: FH");
        Assert.Equal(
            "T2 job appear: No license: FH  |  Preview 10m",
            Tier2ActiveJobDebug.NextLogMessage(hidden, license));
    }
}
