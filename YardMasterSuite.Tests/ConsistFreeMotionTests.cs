using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ConsistFreeMotionTests
{
    private static LocoControlSnapshot LeadOnForward() =>
        new(engineOn: true, reverser: 1f, throttle: 0.4f, brake: 0f);

    [Fact]
    public void Compare_matching_unit_is_none()
    {
        var lead = LeadOnForward();
        var other = lead;
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_matched_partial_brake_is_none()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.5f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.5f);
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_off_unit_is_yellow()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: false, reverser: 0.5f, throttle: 0f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_but_neutral_is_yellow()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 0.5f, throttle: 0f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_from_idle_cab_looking_at_powered_other_is_yellow()
    {
        // Sitting in Neutral trailer: front is on+Forward — soft yellow from both cabs.
        var lead = new LocoControlSnapshot(engineOn: true, reverser: 0.5f, throttle: 0f, brake: 0f);
        var other = LeadOnForward();
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_brake_mismatch_is_red_even_when_other_off()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0f);
        var other = new LocoControlSnapshot(false, 0.5f, 0f, brake: 0.5f, independentBrake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_independent_brake_mismatch_is_red()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0.6f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_matched_independent_brake_is_none()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.3f, independentBrake: 0.4f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.3f, independentBrake: 0.4f);
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_in_gear_wrong_reverser_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 0f, throttle: 0.4f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_in_gear_throttle_mismatch_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 1f, throttle: 0.9f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_in_gear_brake_mismatch_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 1f, throttle: 0.4f, brake: 0.5f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Aggregate_worst_wins_red_over_yellow()
    {
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.Aggregate(FreeMotionSeverity.None, FreeMotionSeverity.None));
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.Aggregate(FreeMotionSeverity.None, FreeMotionSeverity.Yellow));
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.Aggregate(FreeMotionSeverity.Yellow, FreeMotionSeverity.Red));
    }

    [Fact]
    public void Format_empty_when_none()
    {
        Assert.Equal(string.Empty, ConsistFreeMotion.Format(FreeMotionSeverity.None));
        Assert.Equal(string.Empty, ConsistFreeMotion.FormatHud(FreeMotionSeverity.None));
    }

    [Fact]
    public void Format_yellow_and_red_labels()
    {
        Assert.Equal("MU idle", ConsistFreeMotion.Format(FreeMotionSeverity.Yellow));
        Assert.Equal("MU desync", ConsistFreeMotion.Format(FreeMotionSeverity.Red));
        Assert.Contains(ConsistFreeMotion.YellowColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));
        Assert.Contains(ConsistFreeMotion.RedColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));
        Assert.Contains("MU idle", ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));
        Assert.Contains("MU desync", ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));
    }

    [Fact]
    public void ControlsMatch_allows_small_epsilon()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.40f, 0.50f);
        var other = new LocoControlSnapshot(true, 1f, 0.42f, 0.48f);
        Assert.True(ConsistFreeMotion.ControlsMatch(lead, other));
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }
}
