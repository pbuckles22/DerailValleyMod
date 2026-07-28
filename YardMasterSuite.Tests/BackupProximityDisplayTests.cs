using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BackupProximityDisplayTests
{
    [Fact]
    public void Format_never_says_couple_ready()
    {
        Assert.DoesNotContain("Couple ready", BackupProximityDisplay.Format(0.4f, inCoupleRange: true));
        Assert.DoesNotContain("Couple ready", BackupProximityDisplay.FormatHud(0.4f, inCoupleRange: true));
    }

    [Fact]
    public void Format_uses_tenths()
    {
        Assert.Equal("Rear 1.2m", BackupProximityDisplay.Format(1.24f, inCoupleRange: false));
        Assert.Equal("Rear 0.4m", BackupProximityDisplay.Format(0.4f, inCoupleRange: true));
        Assert.Equal("Rear 9.0m", BackupProximityDisplay.Format(9f, inCoupleRange: false));
    }

    [Fact]
    public void Format_supports_front_label()
    {
        Assert.Equal("Front 1.2m", BackupProximityDisplay.Format(1.24f, inCoupleRange: false, tipActive: true, label: "Front"));
        Assert.Equal("Front —", BackupProximityDisplay.Format(null, inCoupleRange: false, tipActive: true, label: "Front"));
        Assert.Contains("Front 0.4m", BackupProximityDisplay.FormatHud(0.4f, inCoupleRange: true, tipActive: true, label: "Front"));
    }

    [Fact]
    public void FormatHud_green_at_or_below_0_5_with_scan()
    {
        Assert.Contains(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(0.0f, inCoupleRange: true));
        Assert.Contains(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(0.5f, inCoupleRange: true));
        // Smoke: 0.8 / 0.9 too far for green after 0.5.11 retune
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(0.8f, inCoupleRange: true));
        Assert.DoesNotContain(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(0.8f, inCoupleRange: true));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(0.9f, inCoupleRange: true));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(1.4f, inCoupleRange: true));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(0.5f, inCoupleRange: false));
        Assert.DoesNotContain(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(0.5f, inCoupleRange: false));
    }

    [Fact]
    public void FormatHud_yellow_through_30m_including_2m()
    {
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(2.0f, inCoupleRange: false));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(5.0f, inCoupleRange: false));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(29.9f, inCoupleRange: false));
        Assert.Contains(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(30.0f, inCoupleRange: false));
        Assert.DoesNotContain(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(2.0f, inCoupleRange: false));
    }

    [Fact]
    public void FormatHud_plain_beyond_30m()
    {
        Assert.DoesNotContain(BackupProximityDisplay.CautionColor, BackupProximityDisplay.FormatHud(30.1f, inCoupleRange: false));
        Assert.DoesNotContain(BackupProximityDisplay.NearColor, BackupProximityDisplay.FormatHud(35.0f, inCoupleRange: false));
        Assert.Equal("Rear 35.0m", BackupProximityDisplay.Format(35f, inCoupleRange: false));
    }

    [Fact]
    public void Format_dash_when_tip_active_but_clearance_unknown()
    {
        Assert.Equal("Rear —", BackupProximityDisplay.Format(null, inCoupleRange: false, tipActive: true));
        Assert.Equal("Rear —", BackupProximityDisplay.Format(200f, inCoupleRange: false, tipActive: true));
        Assert.Equal("Rear 0.0m", BackupProximityDisplay.Format(null, inCoupleRange: true, tipActive: true));
        Assert.Equal(string.Empty, BackupProximityDisplay.Format(null, inCoupleRange: false, tipActive: false));
    }

    [Fact]
    public void IsInCoupleRange_within_scan_range()
    {
        Assert.True(BackupProximityDisplay.IsInCoupleRange(1.5f));
        Assert.True(BackupProximityDisplay.IsInCoupleRange(0.4f));
        Assert.False(BackupProximityDisplay.IsInCoupleRange(1.51f));
        Assert.False(BackupProximityDisplay.IsInCoupleRange(null));
    }

    [Fact]
    public void NormalizeClearance_rounds_to_tenths_and_caps()
    {
        Assert.Equal(15.4f, BackupProximityDisplay.NormalizeClearance(15.44f));
        Assert.Equal(1.2f, BackupProximityDisplay.NormalizeClearance(1.24f));
        Assert.Null(BackupProximityDisplay.NormalizeClearance(81f));
        Assert.Null(BackupProximityDisplay.NormalizeClearance(null));
    }
}
