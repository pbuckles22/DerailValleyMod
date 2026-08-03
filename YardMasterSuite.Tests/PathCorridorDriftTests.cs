using System.Collections.Generic;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class PathCorridorDriftTests
{
    [Fact]
    public void ExpandFillIns_inserts_connector_between_plan_hops()
    {
        var edges = new[]
        {
            new PathEdge("A", "X", cost: 1f),
            new PathEdge("X", "A", cost: 1f),
            new PathEdge("X", "B", cost: 1f),
            new PathEdge("B", "X", cost: 1f),
            // Dijkstra may also keep a direct hop:
            new PathEdge("A", "B", cost: 3f),
            new PathEdge("B", "A", cost: 3f),
        };

        var expanded = PathCorridorDrift.ExpandFillIns(new[] { "A", "B" }, edges);
        Assert.Equal(new[] { "A", "X", "B" }, expanded);
    }

    [Fact]
    public void IsOnRoute_accepts_fill_in_without_expand()
    {
        var edges = new[]
        {
            new PathEdge("A", "X", cost: 1f),
            new PathEdge("X", "A", cost: 1f),
            new PathEdge("X", "B", cost: 1f),
            new PathEdge("B", "X", cost: 1f),
            new PathEdge("A", "B", cost: 1f),
            new PathEdge("B", "A", cost: 1f),
        };

        Assert.True(PathCorridorDrift.IsOnRoute(new[] { "A", "B" }, "X", edges));
        Assert.True(PathCorridorDrift.IsOnRoute(new[] { "A", "B" }, "A", edges));
        Assert.False(PathCorridorDrift.IsOnRoute(new[] { "A", "B" }, "Z", edges));
    }

    [Fact]
    public void IsOnRoute_rejects_wrong_branch_adjacent_only_to_origin()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", cost: 1f),
            new PathEdge("B", "A", cost: 1f),
            new PathEdge("A", "W", cost: 1f),
            new PathEdge("W", "A", cost: 1f),
        };

        // Wrong diverge: on A—W but not between A and B.
        Assert.False(PathCorridorDrift.IsOnRoute(new[] { "A", "B" }, "W", edges));
    }

    [Fact]
    public void ExpandFillIns_preserves_long_corridor_order()
    {
        var edges = new List<PathEdge>
        {
            new("A", "X", cost: 1f),
            new("X", "A", cost: 1f),
            new("X", "B", cost: 1f),
            new("B", "X", cost: 1f),
            new("B", "C", cost: 1f),
            new("C", "B", cost: 1f),
        };

        var expanded = PathCorridorDrift.ExpandFillIns(new[] { "A", "B", "C" }, edges);
        Assert.Equal(new[] { "A", "X", "B", "C" }, expanded);
    }

    [Fact]
    public void JunctionsUnchanged_true_when_no_throws()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            new[] { new PathJunctionEval("J1", 0, 0), new PathJunctionEval("J2", 1, 1) },
            0,
            0,
            false,
            1f);
        var live = new Dictionary<string, int> { ["J1"] = 0, ["J2"] = 1, ["J9"] = 0 };
        var snap = PathCorridorDrift.CaptureJunctionBranches(plan, live);
        Assert.Equal(2, snap.Count);
        Assert.True(PathCorridorDrift.JunctionsUnchanged(snap, live));

        live["J2"] = 0; // thrown
        Assert.False(PathCorridorDrift.JunctionsUnchanged(snap, live));
    }

    [Fact]
    public void JunctionsUnchanged_empty_snapshot_means_no_corridor_switches()
    {
        Assert.True(PathCorridorDrift.JunctionsUnchanged(
            new Dictionary<string, int>(),
            new Dictionary<string, int> { ["J1"] = 1 }));
    }

    [Fact]
    public void PlannedJunctionChanged_true_even_while_train_is_on_corridor()
    {
        var frozen = new Dictionary<string, int> { ["W-0414"] = 0 };
        var live = new Dictionary<string, int> { ["W-0414"] = 1 };

        Assert.True(PathCorridorDrift.PlannedJunctionChanged(frozen, live));
    }

    [Fact]
    public void PlannedJunctionChanged_ignores_unrelated_switch()
    {
        var frozen = new Dictionary<string, int> { ["W-0414"] = 0 };
        var live = new Dictionary<string, int> { ["W-0414"] = 0, ["OTHER"] = 1 };

        Assert.False(PathCorridorDrift.PlannedJunctionChanged(frozen, live));
    }
}
