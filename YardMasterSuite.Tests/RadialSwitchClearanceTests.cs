using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class RadialSwitchClearanceTests
{
    [Fact]
    public void SafeRadius_shortens_baseline_by_nine()
    {
        Assert.Equal(9f, RadialSwitchClearance.SafeRadiusMeters(18f));
        Assert.Equal(9f, RadialSwitchClearance.RadiusShortenMeters);
    }

    [Fact]
    public void PickTrailingTip_takes_lower_projection()
    {
        RadialSwitchClearance.PickTrailingTip(
            tipAx: 0f, tipAz: 20f, tipBx: 0f, tipBz: -5f,
            axisX: 0f, axisZ: 1f,
            out var tx, out var tz);
        Assert.Equal(0f, tx, 3);
        Assert.Equal(-5f, tz, 3);
    }

    [Fact]
    public void ThroughSwitch_far_never_cleared_without_entry()
    {
        var status = RadialSwitchClearance.EvaluateThroughSwitch(
            0, 0, 0, 300, 9f,
            wasInside: false, stickyCleared: false, entryDirX: 0, entryDirZ: 0,
            out var inside, out var sticky, out _, out _);
        Assert.Equal(ConsistClearanceStatus.Fouling, status);
        Assert.False(inside);
        Assert.False(sticky);
    }

    [Fact]
    public void ThroughSwitch_sticky_blowby_ok_reenter_danger_cancels()
    {
        var s = RadialSwitchClearance.EvaluateThroughSwitch(
            0, 0, 0, -5, 9f,
            false, false, 0, 0,
            out var was, out var sticky, out var ex, out var ez);
        Assert.Equal(ConsistClearanceStatus.Fouling, s);
        Assert.True(was);
        Assert.False(sticky);

        // Past clear line → Cleared + sticky.
        s = RadialSwitchClearance.EvaluateThroughSwitch(
            0, 0, 0, 12, 9f, was, sticky, ex, ez,
            out was, out sticky, out ex, out ez);
        Assert.Equal(ConsistClearanceStatus.Cleared, s);
        Assert.True(sticky);

        // Blow past further → still Cleared.
        s = RadialSwitchClearance.EvaluateThroughSwitch(
            0, 0, 0, 40, 9f, was, sticky, ex, ez,
            out was, out sticky, out ex, out ez);
        Assert.Equal(ConsistClearanceStatus.Cleared, s);
        Assert.True(sticky);

        // Forward back onto switch (butt in danger) → At switch again.
        s = RadialSwitchClearance.EvaluateThroughSwitch(
            0, 0, 0, 2, 9f, was, sticky, ex, ez,
            out was, out sticky, out ex, out ez);
        Assert.Equal(ConsistClearanceStatus.Fouling, s);
        Assert.False(sticky);
    }

    [Fact]
    public void Smoke_SwB4L_Far_Latch_Blocks_Cleared()
    {
        const float pinX = 356.065f, pinZ = 879.825f;
        const float b4lX = 568.851f, b4lZ = 546.954f;
        var R = RadialSwitchClearance.SafeRadiusMeters();

        var s = RadialSwitchClearance.EvaluateThroughSwitch(
            pinX, pinZ, b4lX, b4lZ, R,
            false, false, 0, 0,
            out var latch, out var sticky, out _, out _);
        Assert.Equal(ConsistClearanceStatus.Fouling, s);
        Assert.False(latch);
        Assert.False(sticky);
    }

    [Fact]
    public void MetersToClearRimAlongAxis_symmetric_coming_and_going()
    {
        const float R = 9f;
        // 5 m past center toward clear → 4 m to rim.
        Assert.Equal(4, RadialSwitchClearance.MetersToClearRimAlongAxis(5f, R));
        // 5 m on entry side (signed −5) → 14 m to clear rim.
        Assert.Equal(14, RadialSwitchClearance.MetersToClearRimAlongAxis(-5f, R));
        // Already past rim.
        Assert.Equal(0, RadialSwitchClearance.MetersToClearRimAlongAxis(12f, R));
    }

    [Fact]
    public void SignedClearMeters_opposes_entry()
    {
        // Entered from −Z; clear axis is +Z.
        var signed = RadialSwitchClearance.SignedClearMeters(
            0, 0, 0, 12, entryDirX: 0, entryDirZ: -1);
        Assert.Equal(12f, signed, 3);
    }
}
