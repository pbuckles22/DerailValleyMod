using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathPlanTests
{
    [Fact]
    public void Find_prefers_through_lane_over_spur_pocket()
    {
        // A -cheap-> Through -cheap-> B
        // A -expensive-> Pocket -expensive-> B
        var edges = new[]
        {
            new PathEdge("A", "TH", cost: PathTrackCosts.Through),
            new PathEdge("TH", "A", cost: PathTrackCosts.Through),
            new PathEdge("TH", "B", cost: PathTrackCosts.Through),
            new PathEdge("B", "TH", cost: PathTrackCosts.Through),
            new PathEdge("A", "PK", cost: PathTrackCosts.SpurPocket),
            new PathEdge("PK", "A", cost: PathTrackCosts.SpurPocket),
            new PathEdge("PK", "B", cost: PathTrackCosts.SpurPocket),
            new PathEdge("B", "PK", cost: PathTrackCosts.SpurPocket),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B");
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Equal(new[] { "A", "TH", "B" }, plan.TrackIds);
    }

    [Fact]
    public void Find_counts_reverse_hops_and_last_into_dest()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", cost: 1f),
            new PathEdge("B", "A", cost: 1f),
            new PathEdge("B", "STALL", cost: 1f, requiresReverse: true),
            new PathEdge("STALL", "B", cost: 1f, requiresReverse: true),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "STALL");
        Assert.Equal(1, plan.ReverseCount);
        Assert.True(plan.LastHopRequiresReverse);
        Assert.Equal("Reverse into dest", RouteFacingDisplay.Format(plan));
    }

    [Fact]
    public void Facing_OK_when_no_reverses()
    {
        var edges = new[]
        {
            new PathEdge("A", "B"),
            new PathEdge("B", "A"),
        };
        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B");
        Assert.Equal("Facing OK", RouteFacingDisplay.Format(plan));
    }

    [Fact]
    public void RequiredFlips_lists_misaligned_only()
    {
        var edges = new[]
        {
            new PathEdge("S", "B0", "J1", 0),
            new PathEdge("B0", "S", "J1", 0),
        };
        var selected = new Dictionary<string, int> { ["J1"] = 1 };
        var plan = PathPlan.Find(edges, selected, "S", "B0");
        var flips = PathPlan.RequiredFlips(plan);
        Assert.Single(flips);
        Assert.Equal(0, flips[0].RequiredBranch);
    }
}

public class DestinationCatalogTests
{
    private static readonly (string, string)[] Entries =
    {
        ("SM", "SM-O6I"),
        ("SM", "SM-L1"),
        ("FF", "FF-A2P"),
        ("", "bad"),
    };

    [Fact]
    public void ListYards_sorted_unique()
    {
        Assert.Equal(new[] { "FF", "SM" }, DestinationCatalog.ListYards(Entries));
    }

    [Fact]
    public void ListTracksInYard_filtered()
    {
        Assert.Equal(new[] { "SM-L1", "SM-O6I" }, DestinationCatalog.ListTracksInYard(Entries, "SM"));
    }

    [Fact]
    public void CycleIndex_wraps()
    {
        Assert.Equal(0, DestinationCatalog.CycleIndex(2, 3, 1));
        Assert.Equal(2, DestinationCatalog.CycleIndex(0, 3, -1));
    }
}

public class RouteAlignAccessTests
{
    [Fact]
    public void CanAlign_requires_dispatcher()
    {
        Assert.True(RouteAlignAccess.CanAlign(true));
        Assert.False(RouteAlignAccess.CanAlign(false));
        Assert.Equal("Need Dispatcher", RouteAlignAccess.DeniedChip(false));
        Assert.Null(RouteAlignAccess.DeniedChip(true));
    }
}

[Collection("StaticSessions")]
public class RouteDestSessionTests
{
    public RouteDestSessionTests() => RouteDestSession.Clear();

    [Fact]
    public void Set_yard_and_track()
    {
        RouteDestSession.Set("SM", "SM-O6I");
        Assert.True(RouteDestSession.HasDestination);
        Assert.Equal("SM", RouteDestSession.YardId);
        Assert.Equal("SM-O6I", RouteDestSession.TrackId);
        Assert.Equal("SM-O6I", PathCheckSession.DestinationTrackId);
    }
}

public class PathTrackCostsTests
{
    [Fact]
    public void Classify_main_vs_storage()
    {
        Assert.Equal(PathTrackClass.Through, PathTrackCosts.Classify("MAIN_LINE_TYPE"));
        Assert.Equal(PathTrackClass.Through, PathTrackCosts.Classify("LOADING_PASSENGER_TYPE"));
        Assert.Equal(PathTrackClass.SpurPocket, PathTrackCosts.Classify("STORAGE_TYPE"));
        Assert.Equal(PathTrackClass.SpurPocket, PathTrackCosts.Classify("LOADING_TYPE"));
    }
}
