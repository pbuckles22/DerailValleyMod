using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Tier 1 — loco radar FoT only on city enter / leave-loco / force (no timer).
/// </summary>
public class LocoRadarScanPolicyTests
{
    [Fact]
    public void Decide_disabled_never_scans()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: false,
            forceScan: true,
            lastScannedCityId: null,
            currentCityId: "FF",
            lastOccupiedLocoId: 1,
            currentOccupiedLocoId: null,
            out var left);

        Assert.Equal(LocoRadarScanReason.None, reason);
        Assert.Null(left);
    }

    [Fact]
    public void Decide_sitting_in_cab_same_city_no_scan()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 42,
            currentOccupiedLocoId: 42,
            out var left);

        Assert.Equal(LocoRadarScanReason.None, reason);
        Assert.Null(left);
    }

    [Fact]
    public void Decide_city_change_scans_once()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "SM",
            lastOccupiedLocoId: 42,
            currentOccupiedLocoId: 42,
            out _);

        Assert.Equal(LocoRadarScanReason.CityEntered, reason);
    }

    [Fact]
    public void Decide_first_city_seen_scans()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: null,
            currentCityId: "HB",
            lastOccupiedLocoId: null,
            currentOccupiedLocoId: null,
            out _);

        Assert.Equal(LocoRadarScanReason.CityEntered, reason);
    }

    [Fact]
    public void Decide_leave_loco_scans_and_marks_left()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 99,
            currentOccupiedLocoId: null,
            out var left);

        Assert.Equal(LocoRadarScanReason.LeftLoco, reason);
        Assert.Equal(99, left);
    }

    [Fact]
    public void Decide_switch_loco_scans_marks_departed()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 10,
            currentOccupiedLocoId: 20,
            out var left);

        Assert.Equal(LocoRadarScanReason.LeftLoco, reason);
        Assert.Equal(10, left);
    }

    [Fact]
    public void Decide_force_beats_idle()
    {
        Assert.Equal(
            LocoRadarScanReason.Forced,
            LocoRadarScanPolicy.Decide(
                featureEnabled: true,
                forceScan: true,
                lastScannedCityId: "FF",
                currentCityId: "FF",
                lastOccupiedLocoId: 1,
                currentOccupiedLocoId: 1,
                out _));
    }

    [Fact]
    public void Decide_empty_city_does_not_rescan_every_frame()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: null,
            currentCityId: null,
            lastOccupiedLocoId: null,
            currentOccupiedLocoId: null,
            out _);

        Assert.Equal(LocoRadarScanReason.None, reason);
    }
}
