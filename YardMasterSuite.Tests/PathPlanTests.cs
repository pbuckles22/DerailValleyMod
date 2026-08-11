using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathPlanTests
{
    [Fact]
    public void PathPlan_WorldMode_MatchesLegacyDefault()
    {
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
        var selected = new Dictionary<string, int>();

        var legacy = PathPlan.Find(edges, selected, "A", "B");
        var world = PathPlan.Find(edges, selected, "A", "B", mode: PathPlanMode.World);

        Assert.Equal(legacy.Status, world.Status);
        Assert.Equal(legacy.TrackIds, world.TrackIds);
        Assert.Equal(legacy.TotalCost, world.TotalCost);
        Assert.Equal(legacy.MisalignedCount, world.MisalignedCount);
        Assert.Equal(legacy.ReverseCount, world.ReverseCount);
    }

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
        // Drive-set is live cab→pin; stub count is topological only.
        Assert.Equal("Set Reverse (stub 1)", RouteFacingDisplay.Format(plan, isTargetBehind: true));
        Assert.Equal("Set Forward (stub 1)", RouteFacingDisplay.Format(plan, isTargetBehind: false));
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
        Assert.Equal("Set Forward", RouteFacingDisplay.Format(plan, isTargetBehind: false));
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

    /// <summary>
    /// Yard corridor that re-uses one junction with two branches: pin = conflict switch
    /// (approach), not the earlier corridor flip.
    /// </summary>
    [Fact]
    public void Yard_JunctionFirstStop_PinsConflictNotFirstFlip()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J-early", 0, 1f),
            new PathEdge("B", "C", "J-dual", 0, 1f),
            new PathEdge("C", "D", "J-dual", 1, 1f),
            new PathEdge("D", "TT", cost: 1f),
        };
        var selected = new Dictionary<string, int>
        {
            ["J-early"] = 1, // misaligned early flip
            ["J-dual"] = 0,
        };

        var plan = PathPlan.Find(
            edges, selected, "A", "TT", mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.NotNull(plan.JunctionFirstStop);
        Assert.Equal("J-dual", plan.JunctionFirstStop!.Value.JunctionId);
        Assert.Equal(1, plan.JunctionFirstStop.Value.RequiredBranch);
        Assert.Equal("C", plan.JunctionFirstStop.Value.FromTrackId);
        Assert.Equal("J-dual", SwitchListRouteLeg.PickPinJunctionId(plan));
        Assert.NotEqual("J-early", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void World_JunctionFirstStop_NullWhenCommitmentPreventsConflict()
    {
        // Same edges: World hard-skips the conflicting hop → NoPath (or alternate).
        var edges = new[]
        {
            new PathEdge("A", "B", "J-early", 0, 1f),
            new PathEdge("B", "C", "J-dual", 0, 1f),
            new PathEdge("C", "D", "J-dual", 1, 1f),
            new PathEdge("D", "TT", cost: 1f),
        };
        var selected = new Dictionary<string, int>
        {
            ["J-early"] = 0,
            ["J-dual"] = 0,
        };

        var plan = PathPlan.Find(
            edges, selected, "A", "TT", mode: PathPlanMode.World);
        Assert.Equal(PathCheckStatus.NoPath, plan.Status);
        Assert.Null(plan.JunctionFirstStop);
    }

    [Fact]
    public void Find_prefers_faster_time_over_scenic_long_mainline()
    {
        // Harbor loop: long, full 70 km/h. Inland: shorter, slower 40 km/h curves — still wins on ETA.
        var scenic = PathTrackCosts.TravelSeconds(12000f, 70f, PathTrackClass.Through);
        var inland = PathTrackCosts.TravelSeconds(4000f, 40f, PathTrackClass.Through);
        Assert.True(inland < scenic);

        var edges = new[]
        {
            new PathEdge("A", "H1", cost: scenic / 2f),
            new PathEdge("H1", "A", cost: scenic / 2f),
            new PathEdge("H1", "B", cost: scenic / 2f),
            new PathEdge("B", "H1", cost: scenic / 2f),
            new PathEdge("A", "M1", cost: inland / 2f),
            new PathEdge("M1", "A", cost: inland / 2f),
            new PathEdge("M1", "B", cost: inland / 2f),
            new PathEdge("B", "M1", cost: inland / 2f),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B");
        Assert.Equal(new[] { "A", "M1", "B" }, plan.TrackIds);
    }

    [Fact]
    public void Find_prefers_through_yard_over_storage_even_when_storage_shorter()
    {
        // Short storage pocket vs longer free thru — Dijkstra spur penalty (via classFor) wins.
        var edges = new[]
        {
            new PathEdge("A", "TH", cost: PathTrackCosts.TravelSeconds(200f, 40f, PathTrackClass.Through)),
            new PathEdge("TH", "A", cost: PathTrackCosts.TravelSeconds(200f, 40f, PathTrackClass.Through)),
            new PathEdge("TH", "B", cost: PathTrackCosts.TravelSeconds(200f, 40f, PathTrackClass.Through)),
            new PathEdge("B", "TH", cost: PathTrackCosts.TravelSeconds(200f, 40f, PathTrackClass.Through)),
            new PathEdge("A", "PK", cost: PathTrackCosts.TravelSeconds(60f, 70f, PathTrackClass.SpurPocket)),
            new PathEdge("PK", "A", cost: PathTrackCosts.TravelSeconds(60f, 70f, PathTrackClass.SpurPocket)),
            new PathEdge("PK", "B", cost: PathTrackCosts.TravelSeconds(60f, 70f, PathTrackClass.SpurPocket)),
            new PathEdge("B", "PK", cost: PathTrackCosts.TravelSeconds(60f, 70f, PathTrackClass.SpurPocket)),
        };

        PathTrackClass ClassFor(string id) => id == "PK"
            ? PathTrackClass.SpurPocket
            : PathTrackClass.Through;

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B", ClassFor);
        Assert.Equal(new[] { "A", "TH", "B" }, plan.TrackIds);
    }

    [Fact]
    public void Find_at_junction_stem_ignores_cheaper_plain_shortcut()
    {
        // HB throat: cheap plain #Y hop vs junction branches — must use the switch.
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2.2f),
            new PathEdge("#Y-#S1170#T", "#Y-#S623#T", cost: 2.2f),
            new PathEdge("#Y-#S1170#T", "OWC-A1L", cost: 100f),
            new PathEdge("OWC-A1L", "#Y-#S1170#T", cost: 100f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "S-0254-HB", 0, 7.2f),
            new PathEdge("#Y-#S1243#T", "#Y-#S623#T", "S-0254-HB", 0, 7.2f),
            new PathEdge("#Y-#S1243#T", "OWC-A1L", cost: 100f),
            new PathEdge("OWC-A1L", "#Y-#S1243#T", cost: 100f),
            new PathEdge("#Y-#S623#T", "#Y-#S853#T", "S-0254-HB", 1, 7.2f),
            new PathEdge("#Y-#S853#T", "#Y-#S623#T", "S-0254-HB", 1, 7.2f),
            new PathEdge("#Y-#S853#T", "OWC-A1L", cost: 200f),
            new PathEdge("OWC-A1L", "#Y-#S853#T", cost: 200f),
        };

        var plan = PathPlan.Find(
            edges, new Dictionary<string, int>(), "#Y-#S623#T", "OWC-A1L");
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.DoesNotContain("#Y-#S1170#T", plan.TrackIds);
        Assert.Contains("#Y-#S1243#T", plan.TrackIds);
        Assert.Single(plan.Junctions);
        Assert.Equal("S-0254-HB", plan.Junctions[0].JunctionId);
    }

    [Fact]
    public void Find_skips_plain_only_at_origin_stem()
    {
        var edges = new[]
        {
            new PathEdge("ORIGIN", "PLAIN-DEAD", cost: 1f),
            new PathEdge("ORIGIN", "BRANCH", "J-ORIGIN", 0, 2f),
            new PathEdge("ORIGIN", "OTHER-DEAD", "J-ORIGIN", 1, 2f),
            // BRANCH also looks like a multi-branch stem, but its plain continuation is valid.
            new PathEdge("BRANCH", "CONTINUE", cost: 1f),
            new PathEdge("BRANCH", "DEAD-A", "J-DOWNSTREAM", 0, 2f),
            new PathEdge("BRANCH", "DEAD-B", "J-DOWNSTREAM", 1, 2f),
            new PathEdge("CONTINUE", "DEST", cost: 1f),
        };

        var plan = PathPlan.Find(
            edges, new Dictionary<string, int>(), "ORIGIN", "DEST");

        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Equal(new[] { "ORIGIN", "BRANCH", "CONTINUE", "DEST" }, plan.TrackIds);
    }

    [Fact]
    public void Find_bans_reverse_outside_destination_yard()
    {
        // Cheap reverse through intermediate HB; expensive forward loop — must take the loop.
        var edges = new[]
        {
            new PathEdge("FF-A1L", "HB-P1P", cost: 10f, requiresReverse: true),
            new PathEdge("HB-P1P", "OWC-A1L", cost: 10f, requiresReverse: true),
            new PathEdge("FF-A1L", "LOOP", cost: 200f),
            new PathEdge("LOOP", "OWC-A1L", cost: 200f),
            new PathEdge("HB-P1P", "FF-A1L", cost: 10f),
            new PathEdge("OWC-A1L", "HB-P1P", cost: 10f),
            new PathEdge("LOOP", "FF-A1L", cost: 200f),
            new PathEdge("OWC-A1L", "LOOP", cost: 200f),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "FF-A1L", "OWC-A1L");
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Equal(new[] { "FF-A1L", "LOOP", "OWC-A1L" }, plan.TrackIds);
        Assert.Equal(0, plan.ReverseCount);
    }

    /// <summary>
    /// Smoke SW TT: anonymous dest needs an intermediate reverse; without session destYard → NoPath;
    /// with same-town destYard SW → Path OK.
    /// </summary>
    [Fact]
    public void Smoke_SwTurntable_SameTown_AllowsIntermediateReverseToAnonymousDest()
    {
        var edges = new[]
        {
            new PathEdge("SW-B4L", "SW-C1O", cost: 10f),
            new PathEdge("SW-C1O", "SW-B4L", cost: 10f),
            // Must reverse off the through to reach the TT spur (not last-hop-only).
            new PathEdge("SW-C1O", "SW-A2P", cost: 10f, requiresReverse: true),
            new PathEdge("SW-A2P", "SW-C1O", cost: 10f, requiresReverse: true),
            new PathEdge("SW-A2P", "#Y-#S1774#T", cost: 10f),
            new PathEdge("#Y-#S1774#T", "SW-A2P", cost: 10f),
        };

        var banned = PathPlan.Find(
            edges, new Dictionary<string, int>(), "SW-B4L", "#Y-#S1774#T");
        Assert.Equal(PathCheckStatus.NoPath, banned.Status);

        var ok = PathPlan.Find(
            edges,
            new Dictionary<string, int>(),
            "SW-B4L",
            "#Y-#S1774#T",
            destYardId: "SW");
        Assert.Equal(PathCheckStatus.Aligned, ok.Status);
        Assert.Contains("#Y-#S1774#T", ok.TrackIds);
        Assert.True(ok.ReverseCount >= 1);
    }

    [Fact]
    public void Find_same_town_does_not_weaken_intercity_reverse_ban()
    {
        // Cheap reverse through intermediate HB; expensive forward loop — must take the loop
        // even when destYardId is passed (OWC ≠ FF origin).
        var edges = new[]
        {
            new PathEdge("FF-A1L", "HB-P1P", cost: 10f, requiresReverse: true),
            new PathEdge("HB-P1P", "OWC-A1L", cost: 10f, requiresReverse: true),
            new PathEdge("FF-A1L", "LOOP", cost: 200f),
            new PathEdge("LOOP", "OWC-A1L", cost: 200f),
            new PathEdge("HB-P1P", "FF-A1L", cost: 10f),
            new PathEdge("OWC-A1L", "HB-P1P", cost: 10f),
            new PathEdge("LOOP", "FF-A1L", cost: 200f),
            new PathEdge("OWC-A1L", "LOOP", cost: 200f),
        };

        var plan = PathPlan.Find(
            edges, new Dictionary<string, int>(), "FF-A1L", "OWC-A1L", destYardId: "OWC");
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Equal(new[] { "FF-A1L", "LOOP", "OWC-A1L" }, plan.TrackIds);
        Assert.Equal(0, plan.ReverseCount);
    }

    [Fact]
    public void Find_prefers_through_over_yard_service_via_nonthrough_penalty()
    {
        var baseCost = PathTrackCosts.TravelSeconds(100f, 40f, PathTrackClass.Through);
        var edges = new[]
        {
            new PathEdge("A", "YS", cost: baseCost),
            new PathEdge("YS", "B", cost: baseCost),
            new PathEdge("A", "TH", cost: baseCost + 20f),
            new PathEdge("TH", "B", cost: baseCost + 20f),
            new PathEdge("YS", "A", cost: baseCost),
            new PathEdge("B", "YS", cost: baseCost),
            new PathEdge("TH", "A", cost: baseCost + 20f),
            new PathEdge("B", "TH", cost: baseCost + 20f),
        };

        PathTrackClass ClassFor(string id) => id == "YS"
            ? PathTrackClass.YardService
            : PathTrackClass.Through;

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B", ClassFor);
        Assert.Equal(new[] { "A", "TH", "B" }, plan.TrackIds);
    }

    [Fact]
    public void Find_prefers_pull_through_over_reverse_into_siding()
    {
        var pull = PathTrackCosts.TravelSeconds(500f, 40f, PathTrackClass.Through);
        var reverseHop = PathTrackCosts.TravelSeconds(80f, 20f, PathTrackClass.Through);
        var edges = new[]
        {
            new PathEdge("A", "LOOP", cost: pull),
            new PathEdge("LOOP", "A", cost: pull),
            new PathEdge("LOOP", "DEST", cost: pull),
            new PathEdge("DEST", "LOOP", cost: pull),
            new PathEdge("A", "STUB", cost: reverseHop, requiresReverse: true),
            new PathEdge("STUB", "A", cost: reverseHop, requiresReverse: true),
            new PathEdge("STUB", "DEST", cost: reverseHop, requiresReverse: true),
            new PathEdge("DEST", "STUB", cost: reverseHop, requiresReverse: true),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "DEST");
        Assert.Equal(new[] { "A", "LOOP", "DEST" }, plan.TrackIds);
        Assert.Equal(0, plan.ReverseCount);
    }

    [Fact]
    public void RequiredFlips_dedupes_same_junction_keeps_first_along_path()
    {
        var junctions = new[]
        {
            new PathJunctionEval("J1", 0, 1), // first visit — throw to 0
            new PathJunctionEval("J2", 1, 0),
            new PathJunctionEval("J1", 1, 0), // later revisit wants 1 — do not override
        };
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B", "C", "D" },
            junctions,
            3,
            0,
            false,
            10f);

        var flips = PathPlan.RequiredFlips(plan);
        Assert.Equal(2, flips.Count);
        Assert.Equal("J1", flips[0].JunctionId);
        Assert.Equal(0, flips[0].RequiredBranch);
        Assert.Equal("J2", flips[1].JunctionId);
    }

    [Fact]
    public void ReevaluateAlong_same_corridor_after_throws_is_aligned()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J1", 0, PathTrackCosts.HopCost(100f, PathTrackClass.Through)),
            new PathEdge("B", "A", "J1", 0, PathTrackCosts.HopCost(100f, PathTrackClass.Through)),
            new PathEdge("B", "C", "J2", 1, PathTrackCosts.HopCost(100f, PathTrackClass.Through)),
            new PathEdge("C", "B", "J2", 1, PathTrackCosts.HopCost(100f, PathTrackClass.Through)),
        };
        var wrong = new Dictionary<string, int> { ["J1"] = 1, ["J2"] = 0 };
        var found = PathPlan.Find(edges, wrong, "A", "C");
        Assert.Equal(PathCheckStatus.Misaligned, found.Status);
        Assert.Equal(2, found.MisalignedCount);

        var thrown = new Dictionary<string, int> { ["J1"] = 0, ["J2"] = 1 };
        var after = PathPlan.ReevaluateAlong(found.TrackIds, edges, thrown);
        Assert.Equal(PathCheckStatus.Aligned, after.Status);
        Assert.Equal(0, after.MisalignedCount);
        Assert.Equal(found.TrackIds, after.TrackIds);
    }

    [Fact]
    public void Find_skips_corridor_that_needs_both_branches_of_same_junction()
    {
        // W-0416 class bug: cheap path A→M0(J:0)→M1(J:1)→D needs both throws.
        // Legal alternate A→ALT(J2:1)→D must win even when slightly longer.
        var cheap = PathTrackCosts.HopCost(50f, PathTrackClass.Through);
        var mid = PathTrackCosts.HopCost(40f, PathTrackClass.Through);
        var alt = PathTrackCosts.HopCost(200f, PathTrackClass.Through);
        var edges = new[]
        {
            new PathEdge("A", "M0", "J1", 0, cheap),
            new PathEdge("M0", "A", "J1", 0, cheap),
            new PathEdge("M0", "M1", "J1", 1, mid),
            new PathEdge("M1", "M0", "J1", 1, mid),
            new PathEdge("M1", "D", cost: cheap),
            new PathEdge("D", "M1", cost: cheap),
            new PathEdge("A", "ALT", "J2", 1, alt),
            new PathEdge("ALT", "A", "J2", 1, alt),
            new PathEdge("ALT", "D", cost: alt),
            new PathEdge("D", "ALT", cost: alt),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "D");
        Assert.Equal(new[] { "A", "ALT", "D" }, plan.TrackIds);
        Assert.DoesNotContain(plan.Junctions, j => j.JunctionId == "J1");
    }

    [Fact]
    public void ReevaluateAlong_first_junction_branch_wins_no_oscillation()
    {
        // Frozen illegal corridor still present after deploy — Align must converge.
        var edges = new[]
        {
            new PathEdge("A", "M0", "J1", 0, 1f),
            new PathEdge("M0", "A", "J1", 0, 1f),
            new PathEdge("M0", "M1", "J1", 1, 1f),
            new PathEdge("M1", "M0", "J1", 1, 1f),
            new PathEdge("M1", "D", cost: 1f),
            new PathEdge("D", "M1", cost: 1f),
        };
        var selected = new Dictionary<string, int> { ["J1"] = 1 };
        var after = PathPlan.ReevaluateAlong(new[] { "A", "M0", "M1", "D" }, edges, selected);
        Assert.Single(after.Junctions);
        Assert.Equal(0, after.Junctions[0].RequiredBranch);
        Assert.Equal(1, after.MisalignedCount);

        selected["J1"] = 0;
        var clear = PathPlan.ReevaluateAlong(new[] { "A", "M0", "M1", "D" }, edges, selected);
        Assert.Equal(PathCheckStatus.Aligned, clear.Status);
        Assert.Equal(0, clear.MisalignedCount);
        Assert.Empty(PathPlan.RequiredFlips(clear));
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
        Assert.Equal(PathTrackClass.Through, PathTrackCosts.Classify("BLOW_THROUGH_TYPE"));
        Assert.Equal(PathTrackClass.Through, PathTrackCosts.Classify("HB-G3O"));
        Assert.Equal(PathTrackClass.Through, PathTrackCosts.Classify("HB-C2I"));
        Assert.Equal(PathTrackClass.SpurPocket, PathTrackCosts.Classify("STORAGE_TYPE"));
        Assert.Equal(PathTrackClass.SpurPocket, PathTrackCosts.Classify("LOADING_TYPE"));
    }

    [Fact]
    public void TravelSeconds_is_length_over_speed_plus_penalties()
    {
        // 70 km/h = 70/3.6 m/s; 700 m → 36 s
        var main = PathTrackCosts.TravelSeconds(700f, 70f, PathTrackClass.Through);
        Assert.InRange(main, 35f, 37f);

        // Spur travel is base length/speed only; +180s is applied in PathPlan with classFor.
        var spur = PathTrackCosts.TravelSeconds(60f, 70f, PathTrackClass.SpurPocket);
        Assert.True(spur < PathTrackCosts.SpurOccupancyPenaltySeconds);
        Assert.True(spur > 5f);

        var junc = PathTrackCosts.TravelSeconds(
            100f, 70f, PathTrackClass.Through, junctionHop: true);
        Assert.True(junc > PathTrackCosts.TravelSeconds(100f, 70f, PathTrackClass.Through));
    }

    [Fact]
    public void PlanningSpeed_caps_spur_and_yard()
    {
        Assert.Equal(20f, PathTrackCosts.PlanningSpeedKmh(70f, PathTrackClass.SpurPocket));
        Assert.Equal(40f, PathTrackCosts.PlanningSpeedKmh(70f, PathTrackClass.YardService));
        Assert.Equal(70f, PathTrackCosts.PlanningSpeedKmh(70f, PathTrackClass.Through));
    }
}

public class RouteEtaDisplayTests
{
    [Fact]
    public void Format_hms_and_live_chip()
    {
        Assert.Equal("ETA 14m00s", RouteEtaDisplay.Format(14f * 60f));
        Assert.Equal("ETA 20s", RouteEtaDisplay.Format(20f));
        Assert.Equal("ETA 1h05m12s", RouteEtaDisplay.Format(3912f));
        Assert.Equal(
            "Path OK | ETA 20m34s live | rem 840m | trip 62%",
            RouteEtaDisplay.WithPathChip("Path OK", 20f * 60f + 34f, 840f, 0.62f, "live"));
        Assert.Equal("Path OK | ETA 20m34s", RouteEtaDisplay.HudPathChip("Path OK", 20f * 60f + 34f));
        Assert.Equal("ETA 0s", RouteEtaDisplay.Format(0f));
        Assert.Equal(
            "Path OK | ETA 0s arrived | rem 0m | trip 100%",
            RouteEtaDisplay.WithPathChip("Path OK", 0f, 0f, 1f, "arrived"));
        Assert.Null(RouteEtaDisplay.Format(-1f));
    }
}

public class PathRouteDebugTests
{
    [Fact]
    public void Detail_and_costcheck_expose_yards_spur_and_recon()
    {
        var thru = PathTrackCosts.TravelSeconds(200f, 40f, PathTrackClass.Through);
        var edges = new[]
        {
            new PathEdge("FF-A1", "TH", cost: thru),
            new PathEdge("TH", "FF-A1", cost: thru),
            new PathEdge("TH", "SM-L1", cost: thru),
            new PathEdge("SM-L1", "TH", cost: thru),
        };
        PathTrackMeta? Meta(string id) => id switch
        {
            "TH" => new PathTrackMeta(200f, 40f, PathTrackClass.Through),
            "SM-L1" => new PathTrackMeta(200f, 40f, PathTrackClass.Through),
            _ => null,
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "FF-A1", "SM-L1");
        var detail = PathRouteDebug.FormatDetail("set-dest", "FF-A1", "SM-L1", plan, edges, Meta);
        Assert.Contains("T2 path: detail", detail);
        Assert.Contains("yards=FF,SM", detail);
        Assert.Contains("spur=0", detail);
        Assert.Contains("meters=400", detail);

        var check = PathRouteDebug.FormatCostCheck(plan, edges, Meta);
        Assert.Contains("costcheck", check);
        Assert.Contains(" ok", check);
    }

    [Fact]
    public void FormatCorridor_elides_middle()
    {
        var ids = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        Assert.Equal("A>B>C>D>…>G>H>I>J", PathRouteDebug.FormatCorridor(ids, head: 4, tail: 4));
    }

    [Fact]
    public void RemainingCostSeconds_from_mid_corridor()
    {
        var hop = PathTrackCosts.TravelSeconds(100f, 70f, PathTrackClass.Through);
        var edges = new[]
        {
            new PathEdge("A", "B", cost: hop),
            new PathEdge("B", "A", cost: hop),
            new PathEdge("B", "C", cost: hop),
            new PathEdge("C", "B", cost: hop),
            new PathEdge("C", "D", cost: hop),
            new PathEdge("D", "C", cost: hop),
        };
        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "D");
        var full = PathRouteDebug.RemainingCostSeconds(plan, 0, edges);
        var mid = PathRouteDebug.RemainingCostSeconds(plan, 2, edges); // at C → D
        Assert.NotNull(full);
        Assert.NotNull(mid);
        Assert.True(mid < full);
        Assert.InRange(mid!.Value, hop - 0.1f, hop + 0.1f);
        Assert.Equal(0f, PathRouteDebug.RemainingCostSeconds(plan, 3, edges));

        var halfFirst = PathRouteDebug.RemainingCostSeconds(plan, 0, edges, progressAlongFirstHop: 0.5f);
        Assert.NotNull(halfFirst);
        Assert.InRange(halfFirst!.Value, full!.Value - hop * 0.5f - 0.1f, full.Value - hop * 0.5f + 0.1f);
    }

    [Fact]
    public void RemainingMeters_and_live_eta()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B", "C" },
            System.Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            100f);
        PathTrackMeta? Meta(string id) => id switch
        {
            "A" => new PathTrackMeta(1000f, 70f, PathTrackClass.Through),
            "B" => new PathTrackMeta(500f, 70f, PathTrackClass.Through),
            "C" => new PathTrackMeta(200f, 70f, PathTrackClass.Through),
            _ => null,
        };

        // Midway on A: 500 + 500 + 200 = 1200
        Assert.Equal(1200f, PathRouteDebug.RemainingMeters(plan, 0, 0.5f, Meta));
        // 1200 m at 60 km/h = 72 s
        Assert.InRange(PathRouteDebug.LiveEtaSeconds(1200f, 60f)!.Value, 71f, 73f);
        Assert.Null(PathRouteDebug.LiveEtaSeconds(1200f, 5f));

        var smooth = 0f;
        var prev = -1f;
        var planRem = 120f; // plan thinks ~120 s left
        var pace60 = PathRouteDebug.PaceEtaSeconds(1200f, planRem, 60f, ref smooth, ref prev);
        Assert.InRange(pace60, 71f, 73f);
        // Faster than plan → lower ETA; slower (after EMA) → higher. Rate limit applies after first sample.
        var paceSlow = PathRouteDebug.PaceEtaSeconds(1200f, planRem, 20f, ref smooth, ref prev);
        Assert.True(paceSlow > pace60);

        Assert.Equal(0f, PathRouteDebug.TripProgress01(15828f, 15828f));
        Assert.InRange(PathRouteDebug.TripProgress01(1000f, 500f), 0.49f, 0.51f);
        // Drive 142 m on a 15.9 km plan → ~1% trip, not a junction-length jump.
        Assert.Equal(15828f - 142f, PathRouteDebug.RemainingFromDrive(15828f, 142f));
        Assert.InRange(
            PathRouteDebug.TripProgress01(15828f, PathRouteDebug.RemainingFromDrive(15828f, 142f)),
            0.008f,
            0.01f);

        // Trip% from original vs remaining travel ETA (not odometer meters).
        Assert.Equal(0f, PathRouteDebug.TripProgressFromEta(1557f, 1557f));
        Assert.InRange(PathRouteDebug.TripProgressFromEta(1557f, 778.5f), 0.49f, 0.51f);
        Assert.Equal(1f, PathRouteDebug.TripProgressFromEta(1557f, 0f));
        Assert.Equal(1f, PathRouteDebug.TripProgressFromEta(1557f, -5f));

        // Displayed ETA can lead odometer-scaled plan — trip must follow displayed ETA.
        // e.g. full=1311s, odometer planRem=1194s → 9%, displayed 959s → ~27%.
        Assert.InRange(PathRouteDebug.TripProgressFromEta(1311f, 1194f), 0.08f, 0.10f);
        Assert.InRange(PathRouteDebug.TripProgressFromEta(1311f, 959f), 0.26f, 0.28f);
        Assert.InRange(
            PathRouteDebug.RemainingMetersFromEta(14000f, 1311f, 959f),
            10200f,
            10300f);
        Assert.Equal(0f, PathRouteDebug.RemainingMetersFromEta(14000f, 1311f, 0f));

        Assert.True(PathRouteDebug.IsAtDestination("OWC-A1L", "OWC-A1L", plan));
        // Last corridor hop ≠ dest: do not snap rem/ETA early.
        Assert.False(PathRouteDebug.IsAtDestination("C", "OWC-A1L", plan));
        Assert.False(PathRouteDebug.IsAtDestination("A", "OWC-A1L", plan));
        Assert.False(PathRouteDebug.IsAtDestination(null, "OWC-A1L", plan));

        // Odometer may exhaust a short planned length before the dest track —
        // prefer graph rem and never report 0 until arrived.
        Assert.Equal(0f, PathRouteDebug.EffectiveRemainingMeters(0f, null, atDestination: true));
        Assert.Equal(420f, PathRouteDebug.EffectiveRemainingMeters(0f, 420f, atDestination: false));
        Assert.Equal(500f, PathRouteDebug.EffectiveRemainingMeters(500f, 120f, atDestination: false));
        Assert.Equal(1f, PathRouteDebug.EffectiveRemainingMeters(0f, 0f, atDestination: false));
        Assert.Equal(1f, PathRouteDebug.EffectiveRemainingMeters(0f, null, atDestination: false));

        Assert.Equal(0, PathRouteDebug.EarliestCorridorIndex(plan.TrackIds, new[] { "C", "A" }));
        Assert.Equal(2, PathRouteDebug.EarliestCorridorIndex(plan.TrackIds, new[] { "C", "missing" }));
        Assert.Equal(-1, PathRouteDebug.EarliestCorridorIndex(plan.TrackIds, new[] { "nope" }));
    }
}
