using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ThermalThrottleCapTests
{
    private const float MaxCritical = ThermalThrottleCap.DefaultMaxWhenCritical;
    private const float MaxWarning = ThermalThrottleCap.DefaultMaxWhenWarning;

    [Fact]
    public void CeilingForBand_warning_milder_than_critical()
    {
        Assert.Equal(MaxWarning, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Warning));
        Assert.Equal(MaxCritical, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Critical));
        Assert.Equal(MaxCritical, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.WarningAndCritical));
        Assert.Equal(1f, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Nominal));
        Assert.Equal(1f, ThermalThrottleCap.CeilingForBand(null));
    }

    [Fact]
    public void ComputeDesired_passthrough_when_not_hot()
    {
        Assert.Equal(0.85f, ThermalThrottleCap.ComputeDesiredThrottle(0.85f, motorsHot: false, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_hard_snaps_when_delta_zero()
    {
        Assert.Equal(MaxCritical, ThermalThrottleCap.ComputeDesiredThrottle(0.9f, motorsHot: true, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_soft_rolls_toward_warning_ceiling()
    {
        // 0.90 → one second at 5%/s → 0.85 (still above 0.65)
        var stepped = ThermalThrottleCap.ComputeDesiredThrottle(
            0.90f,
            motorsHot: true,
            MaxWarning,
            deltaTime: 1f,
            rollbackPerSecond: 0.05f);
        Assert.Equal(0.85f, stepped, precision: 3);

        // Already at ceiling — hold
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.ComputeDesiredThrottle(MaxWarning, motorsHot: true, MaxWarning, deltaTime: 1f));
    }

    [Fact]
    public void ComputeDesired_soft_roll_does_not_pass_ceiling()
    {
        // 0.78 − 0.05 = 0.73 → clamp up to ceiling 0.75
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.ComputeDesiredThrottle(0.78f, motorsHot: true, MaxWarning, deltaTime: 1f, 0.05f));
    }

    [Fact]
    public void ComputeDesired_leaves_alone_when_hot_but_already_at_or_below_max()
    {
        Assert.Equal(0.25f, ThermalThrottleCap.ComputeDesiredThrottle(0.25f, motorsHot: true, MaxCritical));
        Assert.Equal(MaxCritical, ThermalThrottleCap.ComputeDesiredThrottle(MaxCritical, motorsHot: true, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_never_raises_throttle()
    {
        Assert.Equal(0.1f, ThermalThrottleCap.ComputeDesiredThrottle(0.1f, motorsHot: true, maxWhenHot: 0.9f));
    }

    [Fact]
    public void ComputeDesired_clamps_inputs_to_unit_interval()
    {
        Assert.Equal(0f, ThermalThrottleCap.ComputeDesiredThrottle(-1f, motorsHot: false, MaxCritical));
        Assert.Equal(1f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: false, MaxCritical));
        Assert.Equal(1f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: true, maxWhenHot: 1.5f));
        Assert.Equal(0.4f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: true, maxWhenHot: 0.4f));
    }

    [Fact]
    public void ShouldSoftWrite_only_when_desired_is_lower()
    {
        Assert.True(ThermalThrottleCap.ShouldSoftWrite(0.8f, 0.4f));
        Assert.False(ThermalThrottleCap.ShouldSoftWrite(0.4f, 0.4f));
        Assert.False(ThermalThrottleCap.ShouldSoftWrite(0.3f, 0.4f));
    }

    [Fact]
    public void IsSafeToCap_requires_all_predicates()
    {
        Assert.True(ThermalThrottleCap.IsSafeToCap(
            hasUsableLoco: true,
            controlsPresent: true,
            controlNotBlocked: true,
            motorsHot: true,
            currentAboveCap: true));

        Assert.False(ThermalThrottleCap.IsSafeToCap(false, true, true, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, false, true, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, false, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, true, false, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, true, true, false));
    }
}
