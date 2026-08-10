using System.Collections.Generic;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class PathRouteConstraintsTests
{
    [Fact]
    public void OccupiedSet_dedupes_track_keys()
    {
        var set = PathRouteConstraints.OccupiedSet(new[] { "HB-E5O", " HB-E5O ", null, "HB-A1P" });
        Assert.Equal(2, set.Count);
        Assert.Contains("HB-E5O", set);
        Assert.Contains("HB-A1P", set);
    }

    [Fact]
    public void YardIdOf_named_vs_anonymous()
    {
        Assert.Equal("HB", PathRouteConstraints.YardIdOf("HB-E5O"));
        Assert.Null(PathRouteConstraints.YardIdOf("#Y-#S344#T"));
        Assert.Null(PathRouteConstraints.YardIdOf(null));
    }

    [Fact]
    public void Occupied_non_dest_is_blocked_dest_is_not()
    {
        var occ = PathRouteConstraints.OccupiedSet(new[] { "HB-E5O", "CME-A1P" });
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "HB-E5O", PathTrackClass.Through, occ, "FF-A1L", "CME-A1P"));
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "CME-A1P", PathTrackClass.SpurPocket, occ, "FF-A1L", "CME-A1P"));
    }

    [Fact]
    public void Intermediate_yard_allows_only_empty_through_tracks()
    {
        // PRODUCT LOCK: empty YardService in HB must not be a transit hop.
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "HB-E5O", PathTrackClass.YardService, null, "FF-A1L", "CME-A1P"));
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "HB-E5O", PathTrackClass.Unknown, null, "FF-A1L", "CME-A1P"));
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "HB-G3O", PathTrackClass.Through, null, "FF-A1L", "CME-A1P"));
    }

    [Fact]
    public void Origin_yard_service_blocked_when_leaving_for_another_city()
    {
        // Standing in HB, dest SM — do not Align into HB pocket; Through only.
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "HB-E5O",
            PathTrackClass.YardService,
            occupied: null,
            originTrackId: "HB-A1P",
            destTrackId: "SM-B3I"));
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "HB-G3O",
            PathTrackClass.Through,
            occupied: null,
            originTrackId: "HB-A1P",
            destTrackId: "SM-B3I"));
        // Own origin rail still allowed even if YardService.
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "HB-A1P",
            PathTrackClass.YardService,
            occupied: null,
            originTrackId: "HB-A1P",
            destTrackId: "SM-B3I"));
    }

    [Fact]
    public void Intermediate_through_occupied_blocked()
    {
        var occ = PathRouteConstraints.OccupiedSet(new[] { "HB-G3O" });
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "HB-G3O", PathTrackClass.Through, occ, "FF-A1L", "CME-A1P"));
    }

    [Fact]
    public void Anonymous_junction_backbone_without_yard_alias_not_city_blocked()
    {
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "#Y-#S344#T",
            PathTrackClass.Unknown,
            occupied: null,
            originTrackId: "FF-A1L",
            destTrackId: "CME-A1P"));
    }

    [Fact]
    public void Anonymous_alias_in_intermediate_yard_obeys_through_rule()
    {
        // PRODUCT LOCK: #Y alias of an HB rail must not bypass Through-only.
        string? YardFor(string id) => id == "#Y-#S1294#T" ? "HB" : PathRouteConstraints.YardIdOf(id);

        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "#Y-#S1294#T",
            PathTrackClass.YardService,
            null,
            "FF-A1L",
            "SM-B3I",
            YardFor));
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "#Y-#S1294#T",
            PathTrackClass.Through,
            null,
            "FF-A1L",
            "SM-B3I",
            YardFor));
    }

    [Fact]
    public void No_free_through_in_only_city_yields_no_path()
    {
        // Only hop is occupied Through in intermediate HB — must not use it.
        var edges = new[]
        {
            new PathEdge("FF-A1L", "HB-G3O", cost: 10f),
            new PathEdge("HB-G3O", "CME-A1P", cost: 10f),
            new PathEdge("HB-G3O", "FF-A1L", cost: 10f),
            new PathEdge("CME-A1P", "HB-G3O", cost: 10f),
        };
        var occ = PathRouteConstraints.OccupiedSet(new[] { "HB-G3O" });
        var filtered = PathRouteConstraints.FilterEdges(
            edges, _ => PathTrackClass.Through, occ, "FF-A1L", "CME-A1P");
        var plan = PathPlan.Find(filtered, new Dictionary<string, int>(), "FF-A1L", "CME-A1P");
        Assert.Equal(PathCheckStatus.NoPath, plan.Status);
    }

    [Fact]
    public void ExpandOccupied_one_hop_stub_only_keeps_free_unnamed_lane()
    {
        // Occupied HB-E5O paints only direct #Y neighbor (pocket stub).
        // Free unnamed pass-through #Y-S1243 (no named Track chip) must stay clear.
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "#Y-#S623#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "HB-E5O", cost: 2f),
            new PathEdge("HB-E5O", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "J1", 0, 7f),
            new PathEdge("#Y-#S1243#T", "#Y-#S623#T", "J1", 0, 7f),
            new PathEdge("#Y-#S1243#T", "HB-P1P", cost: 7f),
            new PathEdge("HB-P1P", "#Y-#S1243#T", cost: 7f),
        };

        var named = PathRouteConstraints.OccupiedSet(new[] { "HB-E5O" });
        var expanded = PathRouteConstraints.ExpandOccupiedThroughAnonymous(named, edges);

        Assert.Contains("HB-E5O", expanded);
        Assert.Contains("#Y-#S1170#T", expanded); // 1-hop stub off occupied named
        Assert.DoesNotContain("#Y-#S1243#T", expanded);
        Assert.DoesNotContain("#Y-#S623#T", expanded); // stem not a direct neighbor of HB-E5O
        Assert.DoesNotContain("HB-P1P", expanded);
    }

    [Fact]
    public void ExpandOccupied_skips_dest_so_approach_stubs_stay_open()
    {
        // Job cars on dest FF-C3O must not paint #Y approach — Prep Align would NoPath.
        var edges = new[]
        {
            new PathEdge("#Y-#S725#T", "#Y-#S900#T", cost: 2f),
            new PathEdge("#Y-#S900#T", "#Y-#S725#T", cost: 2f),
            new PathEdge("#Y-#S900#T", "FF-C3O", cost: 2f),
            new PathEdge("FF-C3O", "#Y-#S900#T", cost: 2f),
            new PathEdge("FF-A1S", "#Y-#S111#T", cost: 2f),
            new PathEdge("#Y-#S111#T", "FF-A1S", cost: 2f),
        };

        var named = PathRouteConstraints.OccupiedSet(new[] { "FF-C3O", "FF-A1S" });
        var expanded = PathRouteConstraints.ExpandOccupiedThroughAnonymous(
            named, edges, excludeExpandFrom: "FF-C3O", excludeExpandFrom2: "#Y-#S725#T");

        Assert.Contains("FF-C3O", expanded);
        Assert.Contains("FF-A1S", expanded);
        Assert.DoesNotContain("#Y-#S900#T", expanded); // dest approach stays clear
        Assert.Contains("#Y-#S111#T", expanded); // other occupied named still paints
    }

    [Fact]
    public void ExpandOccupied_then_filter_forces_free_branch()
    {
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "#Y-#S623#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "HB-E5O", cost: 2f),
            new PathEdge("HB-E5O", "#Y-#S1170#T", cost: 2f),
            new PathEdge("HB-E5O", "OWC-A1L", cost: 100f),
            new PathEdge("OWC-A1L", "HB-E5O", cost: 100f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "J1", 0, 7f),
            new PathEdge("#Y-#S1243#T", "#Y-#S623#T", "J1", 0, 7f),
            new PathEdge("#Y-#S1243#T", "HB-P1P", cost: 7f),
            new PathEdge("HB-P1P", "#Y-#S1243#T", cost: 7f),
            new PathEdge("HB-P1P", "OWC-A1L", cost: 100f),
            new PathEdge("OWC-A1L", "HB-P1P", cost: 100f),
        };

        PathTrackClass ClassFor(string id) => id.StartsWith("#")
            ? PathTrackClass.YardService
            : PathTrackClass.Through;

        var named = PathRouteConstraints.OccupiedSet(new[] { "HB-E5O" });
        var expanded = PathRouteConstraints.ExpandOccupiedThroughAnonymous(named, edges);
        var filtered = PathRouteConstraints.FilterEdges(
            edges, ClassFor, expanded, "#Y-#S623#T", "OWC-A1L");
        var plan = PathPlan.Find(
            filtered, new Dictionary<string, int>(), "#Y-#S623#T", "OWC-A1L", ClassFor);

        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Contains("#Y-#S1243#T", plan.TrackIds);
        Assert.Contains("HB-P1P", plan.TrackIds);
        Assert.DoesNotContain("#Y-#S1170#T", plan.TrackIds);
        Assert.DoesNotContain("HB-E5O", plan.TrackIds);
    }

    [Fact]
    public void FilterEdges_drops_occupied_path_takes_through()
    {
        var edges = new[]
        {
            new PathEdge("FF-A1L", "HB-E5O", cost: 10f),
            new PathEdge("HB-E5O", "CME-A1P", cost: 10f),
            new PathEdge("FF-A1L", "HB-P1P", cost: 40f),
            new PathEdge("HB-P1P", "CME-A1P", cost: 40f),
            new PathEdge("HB-E5O", "FF-A1L", cost: 10f),
            new PathEdge("CME-A1P", "HB-E5O", cost: 10f),
            new PathEdge("HB-P1P", "FF-A1L", cost: 40f),
            new PathEdge("CME-A1P", "HB-P1P", cost: 40f),
        };

        PathTrackClass ClassFor(string id) => id switch
        {
            "HB-E5O" => PathTrackClass.YardService,
            "HB-P1P" => PathTrackClass.Through,
            _ => PathTrackClass.Through,
        };

        var occ = PathRouteConstraints.OccupiedSet(new[] { "HB-E5O" });
        var filtered = PathRouteConstraints.FilterEdges(
            edges, ClassFor, occ, "FF-A1L", "CME-A1P");
        var plan = PathPlan.Find(filtered, new Dictionary<string, int>(), "FF-A1L", "CME-A1P", ClassFor);
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Contains("HB-P1P", plan.TrackIds);
        Assert.DoesNotContain("HB-E5O", plan.TrackIds);
    }

    /// <summary>
    /// Smoke SW TT: dest <c>#Y-#S1774#T</c> has no city prefix (dYard=-) — without session yard,
    /// SW YardService rails are Through-only blocked and Path dies. Session SW unlocks them.
    /// </summary>
    [Fact]
    public void Smoke_SwTurntable_AnonymousDest_SessionYard_AllowsYardServiceRails()
    {
        const string dest = "#Y-#S1774#T";
        Assert.Null(PathRouteConstraints.YardIdOf(dest));
        Assert.Equal("SW", PathRouteConstraints.EffectiveDestYardId(dest, "SW"));

        // Without override: origin-yard Through-only blocks SW-A2P.
        Assert.True(PathRouteConstraints.IsEntryBlocked(
            "SW-A2P",
            PathTrackClass.YardService,
            occupied: null,
            originTrackId: "SW-B4L",
            destTrackId: dest));

        // With session dest yard: delivery yard may use service rails.
        Assert.False(PathRouteConstraints.IsEntryBlocked(
            "SW-A2P",
            PathTrackClass.YardService,
            occupied: null,
            originTrackId: "SW-B4L",
            destTrackId: dest,
            yardFor: null,
            destYardOverride: "SW"));
    }

    [Fact]
    public void EffectiveDestYardId_PrefersTrackMeta_OverSession()
    {
        Assert.Equal("MF", PathRouteConstraints.EffectiveDestYardId("MF-B4O", "SW"));
    }
}
