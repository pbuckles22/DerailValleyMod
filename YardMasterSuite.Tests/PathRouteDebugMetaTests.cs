using System.Collections.Generic;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class PathRouteDebugMetaTests
{
    private static PathTrackClass ClassOf(string id) => id switch
    {
        "HB-P1P" => PathTrackClass.Through,
        "HB-E5O" => PathTrackClass.YardService,
        "HB-B3S" => PathTrackClass.SpurPocket,
        _ => PathTrackClass.Unknown,
    };

    [Fact]
    public void CorridorMeta_marks_class_and_occupancy()
    {
        var occupied = new HashSet<string> { "HB-E5O" };
        var text = PathRouteDebug.FormatCorridorMeta(
            new[] { "#Y-#S623#T", "HB-P1P", "HB-E5O", "HB-B3S" },
            ClassOf,
            occupied.Contains);

        Assert.Equal("#Y-#S623#T:Unk HB-P1P:Thru HB-E5O:Yard* HB-B3S:Spur", text);
    }

    [Fact]
    public void CorridorMeta_truncates_with_remaining_count()
    {
        var ids = new[] { "A", "B", "C", "D" };
        var text = PathRouteDebug.FormatCorridorMeta(ids, ClassOf, occupiedOf: null, head: 2);
        Assert.Equal("A:Unk B:Unk …+2", text);
    }

    [Fact]
    public void CorridorMeta_empty_is_dash()
    {
        Assert.Equal("—", PathRouteDebug.FormatCorridorMeta(new string[0], ClassOf, null));
    }

    [Fact]
    public void JunctionCues_flag_misaligned()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B", "C" },
            new[]
            {
                new PathJunctionEval("J1", 0, 1),
                new PathJunctionEval("J2", 1, 1),
            },
            1,
            0,
            false,
            10f);

        Assert.Equal("J1 0/1! J2 1/1", PathRouteDebug.FormatJunctionCues(plan));
    }

    [Fact]
    public void KeySample_head_and_overflow()
    {
        var keys = new[] { "HB-E5O", "HB-B3S", "HB-A1P" };
        Assert.Equal("HB-E5O HB-B3S …+1", PathRouteDebug.FormatKeySample(keys, head: 2));
        Assert.Equal("—", PathRouteDebug.FormatKeySample(new string[0]));
    }

    [Fact]
    public void ThinkHeader_includes_yards_and_candidates()
    {
        var line = PathRouteDebug.FormatThinkHeader(
            "align",
            "HB-A1P",
            "OWC-A1L",
            new[] { "HB-A1P", "#Y-#S623#T" });
        Assert.Contains("splice=HB-A1P", line);
        Assert.Contains("oYard=HB", line);
        Assert.Contains("dYard=OWC", line);
        Assert.Contains("originCands=HB-A1P #Y-#S623#T", line);
    }

    [Fact]
    public void OriginChoices_marks_chosen_and_drops_occupied()
    {
        var edges = new[]
        {
            new PathEdge("HB-A1P", "HB-E5O", cost: 10f),
            new PathEdge("HB-A1P", "HB-P1P", cost: 40f),
        };
        var occ = new HashSet<string> { "HB-E5O" };
        var line = PathRouteDebug.FormatOriginChoices(
            "align",
            "HB-A1P",
            "OWC-A1L",
            edges,
            occ,
            ClassOf,
            chosenNext: "HB-P1P");

        Assert.Contains("*HB-P1P:Thru keep", line);
        Assert.Contains("HB-E5O:Yard* DROP-occ", line);
    }

    [Fact]
    public void OriginChoices_marks_plain_skip_when_junction_outs_exist()
    {
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "S-0254-HB", 0, 7f),
            new PathEdge("#Y-#S623#T", "#Y-#S853#T", "S-0254-HB", 1, 7f),
        };
        var line = PathRouteDebug.FormatOriginChoices(
            "align",
            "#Y-#S623#T",
            "OWC-A1L",
            edges,
            occupied: null,
            classFor: _ => PathTrackClass.YardService,
            chosenNext: "#Y-#S1243#T");

        Assert.Contains("#Y-#S1170#T:Yard SKIP-plain", line);
        Assert.Contains("*#Y-#S1243#T:Yard keep", line);
    }

    [Fact]
    public void ReachProbe_shows_stemOFF_when_only_plain_reaches()
    {
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "OWC-A1L", cost: 50f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "S-0254-HB", 0, 7f),
            new PathEdge("#Y-#S623#T", "#Y-#S853#T", "S-0254-HB", 1, 7f),
            // Junction branches are dead ends (mirrors HB throat when only plain reconnects).
        };
        var line = PathRouteDebug.FormatReachProbe(
            "set",
            "#Y-#S623#T",
            "OWC-A1L",
            edges,
            edges,
            _ => PathTrackClass.YardService);

        Assert.Contains("stemON=NO", line);
        Assert.Contains("stemOFF=YES", line);
        Assert.Contains("via=#Y-#S1170#T[plain]=YES", line);
        Assert.Contains("via=#Y-#S1243#T[J=S-0254-HB:0]=NO", line);
    }

    [Fact]
    public void NearbyTracks_lists_distance_and_occ()
    {
        var near = new[]
        {
            ("#Y-#S1170#T", 12f, PathTrackClass.YardService, false),
            ("HB-A1P", 45f, PathTrackClass.YardService, true),
        };
        var line = PathRouteDebug.FormatNearbyTracks("set", "#Y-#S623#T", near);
        Assert.Contains("#Y-#S1170#T:Yard 12m", line);
        Assert.Contains("HB-A1P:Yard* 45m", line);
    }

    [Fact]
    public void Fanout_shows_plain_and_junction_next()
    {
        var edges = new[]
        {
            new PathEdge("#Y-#S623#T", "#Y-#S1170#T", cost: 2f),
            new PathEdge("#Y-#S1170#T", "#Y-#S619#T", "S-0401-HB", 0, 9f),
            new PathEdge("#Y-#S623#T", "#Y-#S1243#T", "S-0254-HB", 0, 7f),
        };
        var line = PathRouteDebug.FormatOriginFanout(
            "set",
            "#Y-#S623#T",
            edges,
            _ => PathTrackClass.YardService,
            occupied: null);
        Assert.Contains("#Y-#S1170#T:Yard plain -> #Y-#S619#T[J=S-0401-HB:0]", line);
        Assert.Contains("#Y-#S1243#T:Yard J=S-0254-HB:0", line);
    }

    [Fact]
    public void HopThink_shows_step_and_junction()
    {
        var edges = new[]
        {
            new PathEdge("A", "HB-P1P", "J1", 0, 12f),
            new PathEdge("HB-P1P", "B", cost: 20f),
        };
        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B", ClassOf);
        var line = PathRouteDebug.FormatHopThink("align", plan, "B", edges, ClassOf);
        Assert.Contains("1:HB-P1P/Thru", line);
        Assert.Contains("J=J1:0", line);
    }
}
