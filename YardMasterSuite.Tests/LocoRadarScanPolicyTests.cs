using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Driving stutter was rhythmic because loco radar FoT ran on a timer inside town.
/// Product lock: scan once when entering town — never on a 1–3 s cadence.
/// </summary>
public class LocoRadarScanPolicyTests
{
    private static float Sqr(float meters) => meters * meters;

    [Fact]
    public void Driving_the_mainline_past_a_town_never_scans()
    {
        var insideTown = LocoRadarScanPolicy.IsInsideTown(Sqr(900f));
        Assert.False(insideTown);
        Assert.False(LocoRadarScanPolicy.ShouldScan(
            optionEnabled: true,
            insideTown,
            alreadyScannedThisVisit: false));
    }

    [Fact]
    public void Entering_town_scans_once()
    {
        var insideTown = LocoRadarScanPolicy.IsInsideTown(Sqr(200f));
        Assert.True(insideTown);
        Assert.True(LocoRadarScanPolicy.ShouldScan(
            optionEnabled: true,
            insideTown,
            alreadyScannedThisVisit: false));
    }

    [Fact]
    public void Staying_in_town_never_rescans()
    {
        Assert.False(LocoRadarScanPolicy.ShouldScan(
            optionEnabled: true,
            insideTown: true,
            alreadyScannedThisVisit: true));
    }

    [Fact]
    public void Leaving_and_reentering_allows_one_new_scan()
    {
        // Leave clears alreadyScannedThisVisit in the Unity wire-up; Core only sees the next edge.
        Assert.True(LocoRadarScanPolicy.ShouldScan(
            optionEnabled: true,
            insideTown: true,
            alreadyScannedThisVisit: false));
    }

    [Fact]
    public void Option_off_never_scans_even_at_the_station()
    {
        Assert.False(LocoRadarScanPolicy.ShouldScan(
            optionEnabled: false,
            insideTown: true,
            alreadyScannedThisVisit: false));
    }

    [Fact]
    public void Town_edge_is_inclusive_and_junk_distance_is_out()
    {
        Assert.True(LocoRadarScanPolicy.IsInsideTown(
            Sqr(LocoRadarScanPolicy.TownBoundaryRadiusMeters)));
        Assert.False(LocoRadarScanPolicy.IsInsideTown(float.NaN));
        Assert.False(LocoRadarScanPolicy.IsInsideTown(-1f));
    }

    [Fact]
    public void Radar_perf_line_names_scan_cost_and_set_size()
    {
        Assert.Equal(
            "T2 perf radar: scan=42ms cars=310 kept=3",
            HudPerfLog.FormatRadarScan(42, 310, 3));
    }
}
