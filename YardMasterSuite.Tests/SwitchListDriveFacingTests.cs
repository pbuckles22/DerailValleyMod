using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListDriveFacingTests
{
    [Fact]
    public void SetWord_forward_and_reverse()
    {
        Assert.Equal("Set Forward", SwitchListDriveFacing.SetWord(false));
        Assert.Equal("Set Reverse", SwitchListDriveFacing.SetWord(true));
    }

    [Fact]
    public void FormatDriveLabel_includes_set_and_track()
    {
        Assert.Equal(
            "Set Reverse · Pivot → #Y-#S23#T",
            SwitchListDriveFacing.FormatDriveLabel(true, "Pivot", "#Y-#S23#T"));
        Assert.Equal(
            "Set Forward · Turn around → SW-TT",
            SwitchListDriveFacing.FormatDriveLabel(false, "Turn around", "SW-TT"));
    }

    /// <summary>Smoke SW-B4L: pin behind cab forward → Set Reverse (not ReverseCount).</summary>
    [Fact]
    public void IsTargetBehind_detects_dot_product_polarity()
    {
        // Facing North (0, 1)
        Assert.False(DriveSetFacing.IsTargetBehind(0f, 1f, 0f, 10f));
        Assert.True(DriveSetFacing.IsTargetBehind(0f, 1f, 0f, -10f));

        // Facing East (1, 0)
        Assert.False(DriveSetFacing.IsTargetBehind(1f, 0f, 10f, 0f));
        Assert.True(DriveSetFacing.IsTargetBehind(1f, 0f, -10f, 0f));
    }

    [Fact]
    public void RouteFacingDisplay_uses_drive_set_not_stub_count_alone()
    {
        var withStub = new PathPlanResult(
            PathCheckStatus.Aligned,
            System.Array.Empty<string>(),
            System.Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 0f);

        Assert.Equal("Set Forward (stub 1)", RouteFacingDisplay.Format(withStub, isTargetBehind: false));
        Assert.Equal("Set Reverse (stub 1)", RouteFacingDisplay.Format(withStub, isTargetBehind: true));
        Assert.Equal(
            "Set Reverse",
            RouteFacingDisplay.Format(
                new PathPlanResult(
                    PathCheckStatus.Aligned,
                    System.Array.Empty<string>(),
                    System.Array.Empty<PathJunctionEval>(),
                    0,
                    0,
                    false,
                    0f),
                isTargetBehind: true));
    }

    [Fact]
    public void BuildTownTurntable_labels_include_drive_set()
    {
        var steps = SwitchListPlanner.BuildTownTurntable(
            "SW",
            "#Y-#S1774#T",
            "#Y-#S23#T",
            pivotNeedsReverse: true,
            turntableNeedsReverse: false);

        Assert.NotNull(steps);
        Assert.Equal(2, steps!.Count);
        Assert.Equal("Set Reverse · Pivot → #Y-#S23#T", steps[0].Label);
        Assert.Equal("Set Forward · Turn around → #Y-#S1774#T", steps[1].Label);
    }

    [Fact]
    public void BuildTownTurntable_inserts_facing_step_when_direction_flips()
    {
        var steps = SwitchListPlanner.BuildTownTurntable(
            "SW",
            "#Y-#S1774#T",
            "#Y-#S23#T",
            pivotNeedsReverse: false,
            turntableNeedsReverse: true,
            insertFacingBeforeTurntable: true);

        Assert.NotNull(steps);
        Assert.Equal(3, steps!.Count);
        Assert.Equal(SwitchListStepKind.Prep, steps[1].Kind);
        Assert.Equal("Set Reverse", steps[1].Label);
        Assert.Equal("#Y-#S1774#T", steps[1].DestTrackId);
        Assert.StartsWith("Set Reverse · Turn around", steps[2].Label);
    }
}
