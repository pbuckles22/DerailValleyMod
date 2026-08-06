using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 4.13 follow-on: usable named + #Y rails so D-yard → turntable path lines appear.
/// </summary>
public class YardMiniMapTrackSetTests
{
    [Fact]
    public void Smoke_d_yard_to_turntable_includes_anonymous_path_lines()
    {
        // D named → #Y mesh → TT approach (#Y). Foreign HB must not pull in.
        var edges = new List<PathEdge>
        {
            new("D-A1O", "#Y-#S1#T"),
            new("#Y-#S1#T", "D-A1O"),
            new("#Y-#S1#T", "#Y-#S2#T"),
            new("#Y-#S2#T", "#Y-#S1#T"),
            new("#Y-#S2#T", "#Y-#S-tt#T"),
            new("#Y-#S-tt#T", "#Y-#S2#T"),
            new("#Y-#S1#T", "HB-E5O"),
            new("HB-E5O", "#Y-#S1#T"),
        };

        string? YardOf(string key) => key switch
        {
            "D-A1O" or "D-A2P" => "D",
            "HB-E5O" => "HB",
            _ => null,
        };

        var set = YardMiniMapTrackSet.CollectUsableTrackKeys(
            "D",
            seedTrackKeys: new[] { "D-A1O", "D-A2P" },
            edges,
            YardOf);

        Assert.Contains("D-A1O", set);
        Assert.Contains("D-A2P", set);
        Assert.Contains("#Y-#S1#T", set);
        Assert.Contains("#Y-#S2#T", set);
        Assert.Contains("#Y-#S-tt#T", set); // reached via #Y path from named D
        Assert.DoesNotContain("HB-E5O", set);
    }

    [Fact]
    public void Includes_yard_tagged_anonymous_seed_without_named_neighbor()
    {
        string? YardOf(string key) => key == "#Y-#S-d-only#T" ? "D" : null;

        var set = YardMiniMapTrackSet.CollectUsableTrackKeys(
            "D",
            seedTrackKeys: new[] { "#Y-#S-d-only#T" },
            edges: Array.Empty<PathEdge>(),
            YardOf);

        Assert.Contains("#Y-#S-d-only#T", set);
    }

    [Fact]
    public void Does_not_cross_foreign_named_yard()
    {
        var edges = new List<PathEdge>
        {
            new("D-A1O", "#Y-bridge"),
            new("#Y-bridge", "D-A1O"),
            new("#Y-bridge", "HB-E5O"),
            new("HB-E5O", "#Y-bridge"),
            new("HB-E5O", "#Y-hb-only"),
            new("#Y-hb-only", "HB-E5O"),
        };

        string? YardOf(string key) => key switch
        {
            "D-A1O" => "D",
            "HB-E5O" => "HB",
            _ => null,
        };

        var set = YardMiniMapTrackSet.CollectUsableTrackKeys(
            "D",
            seedTrackKeys: new[] { "D-A1O" },
            edges,
            YardOf);

        Assert.Contains("D-A1O", set);
        Assert.Contains("#Y-bridge", set);
        Assert.DoesNotContain("HB-E5O", set);
        Assert.DoesNotContain("#Y-hb-only", set);
    }

    [Fact]
    public void Caps_anonymous_hops()
    {
        var edges = new List<PathEdge>
        {
            new("D-A1O", "#Y-1"),
            new("#Y-1", "D-A1O"),
            new("#Y-1", "#Y-2"),
            new("#Y-2", "#Y-1"),
            new("#Y-2", "#Y-3"),
            new("#Y-3", "#Y-2"),
        };

        string? YardOf(string key) => key == "D-A1O" ? "D" : null;

        var set = YardMiniMapTrackSet.CollectUsableTrackKeys(
            "D",
            seedTrackKeys: new[] { "D-A1O" },
            edges,
            YardOf,
            maxAnonymousHops: 1);

        Assert.Contains("D-A1O", set);
        Assert.Contains("#Y-1", set);
        Assert.DoesNotContain("#Y-2", set);
        Assert.DoesNotContain("#Y-3", set);
    }

    [Fact]
    public void Empty_yard_or_seeds_returns_empty()
    {
        Assert.Empty(
            YardMiniMapTrackSet.CollectUsableTrackKeys(
                null,
                new[] { "D-A1O" },
                Array.Empty<PathEdge>(),
                _ => "D"));
        Assert.Empty(
            YardMiniMapTrackSet.CollectUsableTrackKeys(
                "D",
                Array.Empty<string>(),
                Array.Empty<PathEdge>(),
                _ => "D"));
    }
}
