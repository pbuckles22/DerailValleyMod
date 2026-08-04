using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathCorridorDriftTests
{
    private static PathPlanResult Plan(
        PathCheckStatus status,
        params PathJunctionEval[] junctions) =>
        new(
            status,
            new[] { "A", "B" },
            junctions,
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f);

    [Fact]
    public void CaptureJunctionBranches_prefers_required_over_live()
    {
        var plan = Plan(
            PathCheckStatus.Aligned,
            new PathJunctionEval("J1", requiredBranch: 1, actualBranch: 0));
        var live = new Dictionary<string, int> { ["J1"] = 0 };

        var snap = PathCorridorDrift.CaptureJunctionBranches(plan, live);

        Assert.Equal(1, snap["J1"]);
    }

    [Fact]
    public void JunctionsUnchanged_missing_live_key_is_not_a_throw()
    {
        var snap = new Dictionary<string, int> { ["J1"] = 1, ["J2"] = 0 };
        var live = new Dictionary<string, int> { ["J1"] = 1 };

        Assert.True(PathCorridorDrift.JunctionsUnchanged(snap, live));
        Assert.False(PathCorridorDrift.PlannedJunctionChanged(snap, live));
    }

    [Fact]
    public void JunctionsUnchanged_null_live_is_not_a_throw()
    {
        var snap = new Dictionary<string, int> { ["J1"] = 1 };

        Assert.True(PathCorridorDrift.JunctionsUnchanged(snap, null));
        Assert.False(PathCorridorDrift.PlannedJunctionChanged(snap, null));
    }

    [Fact]
    public void PlannedJunctionChanged_true_when_value_differs()
    {
        var snap = new Dictionary<string, int> { ["J1"] = 1 };
        var live = new Dictionary<string, int> { ["J1"] = 0 };

        Assert.True(PathCorridorDrift.PlannedJunctionChanged(snap, live));
    }

    [Fact]
    public void FormatJunctionDrift_lists_old_to_new()
    {
        var snap = new Dictionary<string, int> { ["J1"] = 1, ["J2"] = 0, ["J3"] = 1 };
        var live = new Dictionary<string, int> { ["J1"] = 0, ["J3"] = 1 };

        var line = PathCorridorDrift.FormatJunctionDrift(snap, live);

        Assert.Contains("J1 1→0", line);
        Assert.Contains("J2 0→?", line);
        Assert.DoesNotContain("J3", line);
    }

    [Fact]
    public void ShouldWatchJunctionDrift_only_when_aligned()
    {
        Assert.True(PathCorridorDrift.ShouldWatchJunctionDrift(PathCheckStatus.Aligned));
        Assert.False(PathCorridorDrift.ShouldWatchJunctionDrift(PathCheckStatus.Misaligned));
        Assert.False(PathCorridorDrift.ShouldWatchJunctionDrift(PathCheckStatus.NoPath));
    }
}
