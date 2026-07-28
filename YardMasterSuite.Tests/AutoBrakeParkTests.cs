using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AutoBrakeParkTests
{
    private const float Rate = AutoBrakePark.DefaultApplyPerSecond;

    [Fact]
    public void DetectEngineOffFallingEdge_only_on_to_off()
    {
        Assert.True(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: true, isEngineOn: false));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: false, isEngineOn: false));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: true, isEngineOn: true));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: false, isEngineOn: true));
    }

    [Fact]
    public void BrakesNeedApply_when_either_below_full()
    {
        Assert.False(AutoBrakePark.BrakesNeedApply(1f, 1f));
        Assert.True(AutoBrakePark.BrakesNeedApply(0.9f, 1f));
        Assert.True(AutoBrakePark.BrakesNeedApply(1f, 0f));
    }

    [Fact]
    public void ThrottleNeedsIdle_when_above_zero()
    {
        Assert.False(AutoBrakePark.ThrottleNeedsIdle(0f));
        Assert.True(AutoBrakePark.ThrottleNeedsIdle(0.1f));
    }

    [Fact]
    public void SessionNeedsWork_brakes_or_throttle()
    {
        Assert.False(AutoBrakePark.SessionNeedsWork(1f, 1f, 0f));
        Assert.True(AutoBrakePark.SessionNeedsWork(0.5f, 1f, 0f));
        Assert.True(AutoBrakePark.SessionNeedsWork(1f, 1f, 0.2f));
    }

    [Fact]
    public void ComputeDesiredBrake_soft_rolls_toward_full()
    {
        Assert.Equal(0.4f, AutoBrakePark.ComputeDesiredBrake(0.4f, applying: false, deltaTime: 1f));
        Assert.Equal(0.5f + Rate, AutoBrakePark.ComputeDesiredBrake(0.5f, applying: true, deltaTime: 1f, Rate), 3);
        Assert.Equal(1f, AutoBrakePark.ComputeDesiredBrake(0.2f, applying: true, deltaTime: 0f));
    }

    [Fact]
    public void ComputeDesiredThrottle_soft_rolls_toward_idle()
    {
        Assert.Equal(0.6f, AutoBrakePark.ComputeDesiredThrottle(0.6f, applying: false, deltaTime: 1f));
        Assert.Equal(0.6f - Rate, AutoBrakePark.ComputeDesiredThrottle(0.6f, applying: true, deltaTime: 1f, Rate), 3);
        Assert.Equal(0f, AutoBrakePark.ComputeDesiredThrottle(0.4f, applying: true, deltaTime: 0f));
        Assert.Equal(0f, AutoBrakePark.ComputeDesiredThrottle(0.1f, applying: true, deltaTime: 1f, 0.2f));
    }

    [Fact]
    public void ShouldRaise_and_ShouldLower()
    {
        Assert.True(AutoBrakePark.ShouldRaise(0.3f, 0.5f));
        Assert.False(AutoBrakePark.ShouldRaise(0.5f, 0.5f));
        Assert.True(AutoBrakePark.ShouldLower(0.5f, 0.3f));
        Assert.False(AutoBrakePark.ShouldLower(0.3f, 0.5f));
    }

    [Fact]
    public void IsSafeToApply_requires_all_predicates()
    {
        Assert.True(AutoBrakePark.IsSafeToApply(true, true, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(false, true, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, false, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, false, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, true, false, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, true, true, false));
    }

    [Fact]
    public void NextPhase_idle_starts_on_falling_edge_when_safe()
    {
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, true, true, true, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, true, true, false, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, false, true, true, true));
    }

    [Fact]
    public void NextPhase_applying_ends_when_done_or_engine_on()
    {
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, true, true, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, true, true, false));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, false, true, true));
    }
}
