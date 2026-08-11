using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class ConsistSwitchClearanceTests
{
    [Fact]
    public void ModeForStep_delivery_cars_only_else_full_train()
    {
        Assert.Equal(ConsistClearanceMode.CarsOnly, ConsistSwitchClearance.ModeForStep(SwitchListStepKind.Delivery));
        Assert.Equal(ConsistClearanceMode.FullTrain, ConsistSwitchClearance.ModeForStep(SwitchListStepKind.Transit));
        Assert.Equal(ConsistClearanceMode.FullTrain, ConsistSwitchClearance.ModeForStep(SwitchListStepKind.TurnAround));
        Assert.Equal(ConsistClearanceMode.FullTrain, ConsistSwitchClearance.ModeForStep(SwitchListStepKind.Prep));
    }

    [Fact]
    public void NotOccupying_cleared_when_entirely_before_or_past()
    {
        // Entirely before switch (approaching)
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.EvaluateNotOccupying(
                0f, 0f, 0f, -80f, 0f, -20f, 0f, 1f));

        // Entirely past
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.EvaluateNotOccupying(
                0f, 0f, 0f, 20f, 0f, 80f, 0f, 1f));
    }

    [Fact]
    public void NotOccupying_fouling_when_straddling()
    {
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.EvaluateNotOccupying(
                0f, 0f, 0f, -40f, 0f, 20f, 0f, 1f));
        Assert.False(ConsistSwitchClearance.IsSafeToThrow(ConsistClearanceStatus.Fouling));
        Assert.True(ConsistSwitchClearance.IsSafeToThrow(ConsistClearanceStatus.Cleared));
    }

    [Fact]
    public void Forward_loco_past_but_rear_fouling_not_past()
    {
        // Travel +Z; switch at origin; loco at +20, rear (10 cars) at -40
        var status = ConsistSwitchClearance.EvaluatePastSwitch(
            switchX: 0f,
            switchZ: 0f,
            tipAx: 0f,
            tipAz: 20f,
            tipBx: 0f,
            tipBz: -40f,
            travelX: 0f,
            travelZ: 1f);
        Assert.Equal(ConsistClearanceStatus.Fouling, status);
        Assert.False(ConsistSwitchClearance.IsArrived(status));
    }

    [Fact]
    public void Forward_rear_past_switch_cleared()
    {
        var status = ConsistSwitchClearance.EvaluatePastSwitch(
            0f,
            0f,
            tipAx: 0f,
            tipAz: 80f,
            tipBx: 0f,
            tipBz: 5f,
            travelX: 0f,
            travelZ: 1f);
        Assert.Equal(ConsistClearanceStatus.Cleared, status);
        Assert.True(ConsistSwitchClearance.IsArrived(status));
    }

    [Fact]
    public void Reverse_front_must_clear_when_backing()
    {
        // Travel −Z (reverse); tips at +10 (still ahead of motion) and −5
        var fouling = ConsistSwitchClearance.EvaluatePastSwitch(
            0f,
            0f,
            tipAx: 0f,
            tipAz: 10f,
            tipBx: 0f,
            tipBz: -5f,
            travelX: 0f,
            travelZ: -1f);
        Assert.Equal(ConsistClearanceStatus.Fouling, fouling);

        var cleared = ConsistSwitchClearance.EvaluatePastSwitch(
            0f,
            0f,
            tipAx: 0f,
            tipAz: -20f,
            tipBx: 0f,
            tipBz: -50f,
            travelX: 0f,
            travelZ: -1f);
        Assert.Equal(ConsistClearanceStatus.Cleared, cleared);
    }

    [Fact]
    public void CarsInZone_both_tips_required_loco_irrelevant()
    {
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.EvaluateCarsInZone(
                zoneX: 100f,
                zoneZ: 100f,
                radiusMeters: 25f,
                carTipAx: 105f,
                carTipAz: 100f,
                carTipBx: 95f,
                carTipBz: 100f));

        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.EvaluateCarsInZone(
                100f,
                100f,
                25f,
                carTipAx: 105f,
                carTipAz: 100f,
                carTipBx: 200f,
                carTipBz: 100f));
    }

    [Fact]
    public void CombinePastAndNear_blocks_false_arrived_when_far_from_pin()
    {
        var past = ConsistClearanceStatus.Cleared;
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.CombinePastAndNear(
                past,
                pinX: 0f,
                pinZ: 0f,
                refX: 0f,
                refZ: 200f,
                nearRadiusMeters: 40f));

        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.CombinePastAndNear(
                past,
                pinX: 0f,
                pinZ: 0f,
                refX: 0f,
                refZ: 10f,
                nearRadiusMeters: 40f));
    }

    /// <summary>
    /// Smoke SW TT: mid-switch (past=Fouling) must not CLEARED even when near the frog.
    /// CLEARED = past gate (trailing tip clear) AND near the junction pin.
    /// </summary>
    [Fact]
    public void Smoke_SwitchCleared_RequiresPastAndNear()
    {
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.CombinePastAndNear(
                ConsistClearanceStatus.Fouling,
                pinX: 0f,
                pinZ: 0f,
                refX: 0f,
                refZ: 6.5f,
                nearRadiusMeters: 35f));

        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.CombinePastAndNear(
                ConsistClearanceStatus.Cleared,
                pinX: 0f,
                pinZ: 0f,
                refX: 0f,
                refZ: 6.5f,
                nearRadiusMeters: 35f));
    }

    [Fact]
    public void EvaluatePastSwitch_gate_margin_blocks_mid_switch()
    {
        // Tips straddle frog: trailing still behind — Fouling at 12 m gate margin.
        Assert.Equal(
            ConsistClearanceStatus.Fouling,
            ConsistSwitchClearance.EvaluatePastSwitch(
                switchX: 0f,
                switchZ: 0f,
                tipAx: -5f,
                tipAz: 0f,
                tipBx: 8f,
                tipBz: 0f,
                travelX: 1f,
                travelZ: 0f,
                marginMeters: ConsistSwitchClearance.SwitchClearGateMarginMeters));

        // Entire consist past frog by ≥12 m.
        Assert.Equal(
            ConsistClearanceStatus.Cleared,
            ConsistSwitchClearance.EvaluatePastSwitch(
                switchX: 0f,
                switchZ: 0f,
                tipAx: 14f,
                tipAz: 0f,
                tipBx: 22f,
                tipBz: 0f,
                travelX: 1f,
                travelZ: 0f,
                marginMeters: ConsistSwitchClearance.SwitchClearGateMarginMeters));
    }

    [Fact]
    public void Unknown_when_travel_zero()
    {
        Assert.Equal(
            ConsistClearanceStatus.Unknown,
            ConsistSwitchClearance.EvaluatePastSwitch(0, 0, 1, 0, -1, 0, 0, 0));
    }
}

public class SwitchListRouteLegTests
{
    [Fact]
    public void PickPinJunctionId_first_misaligned()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B", "C" },
            new[]
            {
                new PathJunctionEval("J1", 1, 1),
                new PathJunctionEval("J2", 0, 1),
                new PathJunctionEval("J3", 1, 0),
            },
            2,
            0,
            false,
            10f);
        Assert.Equal("J2", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void PickPinJunctionId_null_when_clear()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A" },
            new[] { new PathJunctionEval("J1", 0, 0) },
            0,
            0,
            false,
            0f);
        Assert.Null(SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void FilterSafeToThrowFlips_keeps_only_not_occupying()
    {
        var flips = new[]
        {
            new PathJunctionEval("A", 1, 0),
            new PathJunctionEval("B", 1, 0),
        };
        var kept = SwitchListRouteLeg.FilterSafeToThrowFlips(
            flips,
            id => id == "B" ? ConsistClearanceStatus.Cleared : ConsistClearanceStatus.Fouling);
        Assert.Single(kept);
        Assert.Equal("B", kept[0].JunctionId);
    }
}
