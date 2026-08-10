using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteFirstPivotTests
{
    [Fact]
    public void Pick_FailsClosed_OnEmpty()
    {
        Assert.Null(RouteFirstPivot.Pick("SW-B4L", "#Y-TT", null));
        Assert.Null(RouteFirstPivot.Pick("SW-B4L", "#Y-TT", Array.Empty<RoutePivotCandidate>()));
        Assert.Null(RouteFirstPivot.Pick(null, "#Y-TT", new[]
        {
            new RoutePivotCandidate("SW-C1O", true, true, 10f, 50f),
        }));
    }

    /// <summary>
    /// Smoke SW TT multi-step: prefer a bridge pivot (origin→pivot and pivot→TT) over a closer dead-end.
    /// </summary>
    [Fact]
    public void Smoke_SwTt_PrefersBridgePivot_OverCloserDeadEnd()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("SW-DEAD", canReachFromOrigin: true, canReachFinal: false, originToPivotCost: 5f, metersToFinal: 40f),
            new RoutePivotCandidate("SW-C1O", canReachFromOrigin: true, canReachFinal: true, originToPivotCost: 80f, metersToFinal: 120f),
            new RoutePivotCandidate("SW-A2P", canReachFromOrigin: true, canReachFinal: true, originToPivotCost: 40f, metersToFinal: 90f),
        };

        Assert.Equal("SW-A2P", RouteFirstPivot.Pick("SW-B4L", "#Y-#S1774#T", candidates));
    }

    [Fact]
    public void Pick_FallsBack_ToNearestPull_WhenNoBridge()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("SW-FAR", true, false, 10f, 200f),
            new RoutePivotCandidate("SW-NEAR", true, false, 20f, 60f),
            new RoutePivotCandidate("SW-BLOCKED", false, true, 1f, 10f),
        };

        Assert.Equal("SW-NEAR", RouteFirstPivot.Pick("SW-B4L", "#Y-TT", candidates));
    }

    [Fact]
    public void Pick_Skips_OriginAndFinal()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("SW-B4L", true, true, 0f, 200f),
            new RoutePivotCandidate("#Y-TT", true, true, 1f, 0f),
            new RoutePivotCandidate("SW-C1O", true, true, 50f, 100f),
        };

        Assert.Equal("SW-C1O", RouteFirstPivot.Pick("SW-B4L", "#Y-TT", candidates));
    }
}
