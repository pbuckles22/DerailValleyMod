using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class RoutePlanSessionTests
{
    public RoutePlanSessionTests()
    {
        RoutePlanSession.Clear();
        RouteMemo.Clear();
        RouteDestSession.Clear();
    }

    [Fact]
    public void SetPlan_then_stale_hides_plan()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            System.Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            1f);
        RoutePlanSession.SetPlan(plan, "A");
        Assert.True(RoutePlanSession.HasPlan);
        Assert.Equal("A", RoutePlanSession.PlannedOriginTrackId);
        Assert.Null(RoutePlanSession.ExitCue);

        RoutePlanSession.SetPlan(plan, "A", "Exit NE");
        Assert.Equal("Exit NE", RoutePlanSession.ExitCue);

        RoutePlanSession.MarkStale("left planned path");
        Assert.False(RoutePlanSession.HasPlan);
        Assert.True(RoutePlanSession.IsStale);
        Assert.Equal("left planned path", RoutePlanSession.StatusMessage);
        Assert.Null(RoutePlanSession.Plan);
    }

    [Fact]
    public void ContainsTrack_corridor_membership()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B", "C" },
            System.Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            1f);
        Assert.True(plan.ContainsTrack("B"));
        Assert.False(plan.ContainsTrack("Z"));
        Assert.False(plan.ContainsTrack(null));
    }

    [Fact]
    public void RouteMemo_round_trips()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "X", "Y" },
            System.Array.Empty<PathJunctionEval>(),
            2,
            0,
            false,
            5f);
        RouteMemo.Put("X", "Y", plan);
        Assert.True(RouteMemo.TryGet("X", "Y", out var hit));
        Assert.Same(plan, hit);
        Assert.False(RouteMemo.TryGet("X", "Z", out _));
    }
}

public class RouteExitDisplayTests
{
    [Fact]
    public void Format_exit_compass()
    {
        // Toward +Z = north
        Assert.Equal("Exit N", RouteExitDisplay.Format(0, 0, 0, 10));
        // Toward +X = east
        Assert.Equal("Exit E", RouteExitDisplay.Format(0, 0, 10, 0));
        Assert.Null(RouteExitDisplay.Format(0, 0, 0, 0));
    }
}
