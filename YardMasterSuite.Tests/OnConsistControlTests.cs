using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class OnConsistControlTests
{
    [Fact]
    public void ResolveFrontLocoIndex_null_when_player_off_consist()
    {
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: false, new[] { 0 }));
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: false, System.Array.Empty<int>()));
    }

    [Fact]
    public void ResolveFrontLocoIndex_null_when_no_loco_on_trainset()
    {
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, System.Array.Empty<int>()));
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, null!));
    }

    [Fact]
    public void ResolveFrontLocoIndex_picks_lowest_trainset_index()
    {
        Assert.Equal(0, OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, new[] { 3, 0, 2 }));
        Assert.Equal(2, OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, new[] { 5, 2 }));
    }

    [Fact]
    public void ResolveFrontLocoIndex_works_when_player_on_butt_car()
    {
        // Player on last freight (index 8); front loco still index 0.
        Assert.Equal(0, OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, new[] { 0 }));
    }

    [Fact]
    public void Nudge_ramps_toward_clamp_01()
    {
        var rate = 0.10f;
        Assert.Equal(0.20f, OnConsistControl.Nudge(0.10f, direction: +1, deltaTime: 1f, ratePerSecond: rate), 3);
        Assert.Equal(0.05f, OnConsistControl.Nudge(0.15f, direction: -1, deltaTime: 1f, ratePerSecond: rate), 3);
        Assert.Equal(1f, OnConsistControl.Nudge(0.95f, direction: +1, deltaTime: 1f, ratePerSecond: rate), 3);
        Assert.Equal(0f, OnConsistControl.Nudge(0.05f, direction: -1, deltaTime: 1f, ratePerSecond: rate), 3);
        Assert.Equal(0.40f, OnConsistControl.Nudge(0.40f, direction: 0, deltaTime: 1f, ratePerSecond: rate), 3);
    }

    [Fact]
    public void IsSafeToWrite_requires_on_consist_armed_predicates()
    {
        Assert.True(OnConsistControl.IsSafeToWrite(
            worldActive: true,
            playerOnCar: true,
            hasFrontLoco: true,
            controlsPresent: true,
            controlNotBlocked: true));

        Assert.False(OnConsistControl.IsSafeToWrite(false, true, true, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, false, true, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, false, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, true, false, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, true, true, false));
    }

    [Fact]
    public void HudLegend_points_at_cab_bindings()
    {
        var line = OnConsistControl.HudLegend;
        Assert.Contains("Throttle", line);
        Assert.Contains("Indy", line);
        Assert.Contains("TrainBrake", line);
        Assert.Contains("Reverser", line);
        Assert.Contains("TM fuse", line);
        Assert.Contains("front loco", line);
        Assert.DoesNotContain("-=", line);
    }

    [Fact]
    public void ShouldRedirectToFrontLoco_only_when_on_consist_not_in_front_cab()
    {
        Assert.True(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: true, standingIsFrontLoco: false));
        Assert.False(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: true, standingIsFrontLoco: true));
        Assert.False(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: false, standingIsFrontLoco: false));
    }

    [Fact]
    public void StepReverser_notches_R_N_F()
    {
        Assert.Equal(0.5f, OnConsistControl.StepReverser(0f, direction: +1), 3);
        Assert.Equal(1f, OnConsistControl.StepReverser(0.5f, direction: +1), 3);
        Assert.Equal(1f, OnConsistControl.StepReverser(1f, direction: +1), 3);
        Assert.Equal(0.5f, OnConsistControl.StepReverser(1f, direction: -1), 3);
        Assert.Equal(0f, OnConsistControl.StepReverser(0.5f, direction: -1), 3);
        Assert.Equal(0.5f, OnConsistControl.StepReverser(0.5f, direction: 0), 3);
    }

    [Fact]
    public void StepLever_matches_cab_notch_and_remote_unnotched_step()
    {
        // Cab NotchedPortIncrementalInput: 1/(notchCount-1). notchCount=10 → ~11.1% per press.
        Assert.Equal(1f / 9f, OnConsistControl.StepLever(0f, +1, isNotched: true, notchCount: 10f), 3);
        Assert.Equal(2f / 9f, OnConsistControl.StepLever(1f / 9f, +1, isNotched: true, notchCount: 10f), 3);
        Assert.Equal(0.1f, OnConsistControl.StepLever(0f, +1, isNotched: false, notchCount: 1f), 3);
        Assert.Equal(0f, OnConsistControl.StepLever(0.1f, -1, isNotched: false, notchCount: 1f), 3);
    }

    [Fact]
    public void HoldRepeat_fires_on_press_then_after_delay_while_held()
    {
        var next = 0f;
        Assert.True(HoldRepeat.ShouldFire(pressedThisFrame: true, isHeld: true, timeHeld: 0f, ref next));
        Assert.Equal(HoldRepeat.DefaultInitialDelaySeconds, next, 3);

        // Still in the initial delay — no second step.
        Assert.False(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: true, timeHeld: 0.20f, ref next));

        // Delay elapsed — auto-repeat.
        Assert.True(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: true, timeHeld: 0.35f, ref next));
        Assert.Equal(0.35f + HoldRepeat.DefaultIntervalSeconds, next, 3);

        Assert.True(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: true, timeHeld: next, ref next));

        // Release clears schedule.
        Assert.False(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: false, timeHeld: 1f, ref next));
        Assert.Equal(0f, next);
    }

    [Fact]
    public void Toggle01_flips_fuse_state()
    {
        Assert.Equal(0f, OnConsistControl.Toggle01(1f));
        Assert.Equal(1f, OnConsistControl.Toggle01(0f));
        Assert.Equal(0f, OnConsistControl.Toggle01(0.6f));
        Assert.Equal(1f, OnConsistControl.Toggle01(0.4f));
    }

    [Fact]
    public void CanWriteLever_ignores_cab_reach_blocker_when_present()
    {
        // Blocked=true is the butt-car case — still allow soft-write.
        Assert.True(OnConsistControl.CanWriteLever(controlPresent: true, controlBlocked: true));
        Assert.True(OnConsistControl.CanWriteLever(controlPresent: true, controlBlocked: false));
        Assert.False(OnConsistControl.CanWriteLever(controlPresent: false, controlBlocked: false));
    }
}
