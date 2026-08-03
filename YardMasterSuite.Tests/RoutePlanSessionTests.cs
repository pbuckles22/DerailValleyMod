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

        Assert.Equal(1f, RoutePlanSession.EtaCostSeconds);
        RoutePlanSession.SetRemainingEta(0.4f, 800f, 1600f, 0.5f, 0.62f, "live");
        Assert.Equal(0.4f, RoutePlanSession.EtaCostSeconds);
        Assert.Equal(800f, RoutePlanSession.RemainingMeters);
        Assert.Equal(0.5f, RoutePlanSession.TripProgress01);
        Assert.Equal(0.62f, RoutePlanSession.HopProgress01);
        Assert.Equal("live", RoutePlanSession.EtaMode);

        RoutePlanSession.MarkStale("left planned path");
        Assert.False(RoutePlanSession.HasPlan);
        Assert.True(RoutePlanSession.IsStale);
        Assert.Equal("left planned path", RoutePlanSession.StatusMessage);
        Assert.Null(RoutePlanSession.Plan);
        Assert.Null(RoutePlanSession.EtaCostSeconds);
    }

    [Fact]
    public void SetPlan_uses_physical_travel_eta_not_dijkstra_score()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            System.Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            totalCost: 3982f);

        RoutePlanSession.SetPlan(plan, "A", travelEtaSeconds: 2002f);

        Assert.Equal(2002f, RoutePlanSession.PlannedTravelSeconds);
        Assert.Equal(2002f, RoutePlanSession.EtaCostSeconds);
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
