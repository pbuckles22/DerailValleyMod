using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LocoRadarSelectionTests
{
    [Fact]
    public void RankNearest_empty_returns_zero()
    {
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(
            Array.Empty<LocoRadarCandidate>(),
            excludeIds: null,
            maxResults: 3,
            rankedIds: dest);
        Assert.Equal(0, n);
    }

    [Fact]
    public void RankNearest_orders_by_distance_ascending()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(10, distanceSq: 400f),
            new LocoRadarCandidate(20, distanceSq: 100f),
            new LocoRadarCandidate(30, distanceSq: 225f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, 3, dest);
        Assert.Equal(3, n);
        Assert.Equal(new[] { 20, 30, 10 }, dest);
    }

    [Fact]
    public void RankNearest_respects_maxResults()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 9f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 1f),
            new LocoRadarCandidate(4, 16f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, maxResults: 2, dest);
        Assert.Equal(2, n);
        Assert.Equal(3, dest[0]);
        Assert.Equal(2, dest[1]);
    }

    [Fact]
    public void RankNearest_skips_excluded_ids()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 1f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 9f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(
            candidates,
            excludeIds: new HashSet<int> { 1 },
            maxResults: 3,
            rankedIds: dest);
        Assert.Equal(2, n);
        Assert.Equal(new[] { 2, 3 }, dest.Take(2).ToArray());
    }

    [Fact]
    public void RankNearest_ignores_non_positive_maxResults()
    {
        var candidates = new[] { new LocoRadarCandidate(1, 1f) };
        var dest = new int[1];
        Assert.Equal(0, LocoRadarSelection.RankNearest(candidates, null, 0, dest));
        Assert.Equal(0, LocoRadarSelection.RankNearest(candidates, null, -1, dest));
    }

    [Fact]
    public void RankNearest_does_not_overflow_destination()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 1f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 9f),
        };
        var dest = new int[1];
        var n = LocoRadarSelection.RankNearest(candidates, null, maxResults: 3, rankedIds: dest);
        Assert.Equal(1, n);
        Assert.Equal(1, dest[0]);
    }
}

public class LocoRadarDisplayTests
{
    [Fact]
    public void FormatCaption_distance_only_when_no_type_or_place()
    {
        Assert.Equal("120m", LocoRadarDisplay.FormatCaption(null, 120.4f, null));
        Assert.Equal("0m", LocoRadarDisplay.FormatCaption("  ", -5f, ""));
    }

    [Fact]
    public void FormatCaption_type_and_distance()
    {
        Assert.Equal("DE2 145m", LocoRadarDisplay.FormatCaption("LocoDE2", 145.2f, null));
        Assert.Equal("DE6 10m", LocoRadarDisplay.FormatCaption("DE6", 9.6f, null));
    }

    [Fact]
    public void FormatCaption_includes_place_when_present()
    {
        Assert.Equal("DE2 145m SM-O6I", LocoRadarDisplay.FormatCaption("LocoDE2", 145f, "SM-O6I"));
        Assert.Equal("145m C-06S", LocoRadarDisplay.FormatCaption(null, 145f, " C-06S "));
        Assert.Equal("DE6 2042m HB", LocoRadarDisplay.FormatCaption("LocoDE6", 2042f, "HB"));
    }

    [Fact]
    public void TrackIncludesCity_detects_SM_style()
    {
        Assert.True(LocoRadarDisplay.TrackIncludesCity("SM-T12P"));
        Assert.True(LocoRadarDisplay.TrackIncludesCity("FF-A1"));
        Assert.True(LocoRadarDisplay.TrackIncludesCity("HB-O6I"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("#Y"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("Y"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("T12P"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity(null));
    }

    [Fact]
    public void FormatPlace_adds_city_when_track_lacks_city()
    {
        Assert.Equal("SM-T12P", LocoRadarDisplay.FormatPlace("SM-T12P", "SM"));
        Assert.Equal("FF #Y", LocoRadarDisplay.FormatPlace("#Y", "FF"));
        Assert.Equal("FF", LocoRadarDisplay.FormatPlace(null, "FF"));
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", null));
        Assert.Equal("HB", LocoRadarDisplay.FormatPlace("", "HB"));
    }

    [Fact]
    public void FormatPlace_rejects_junk_yardId_matching_spur_track()
    {
        // Game often sets yardId == "#Y" for spur tracks — must not collapse to track-only.
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", "#Y"));
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", "Y"));
        Assert.False(LocoRadarDisplay.IsUsableCityYardId("#Y"));
        Assert.False(LocoRadarDisplay.IsUsableCityYardId("Y"));
        Assert.True(LocoRadarDisplay.IsUsableCityYardId("FF"));
        Assert.True(LocoRadarDisplay.IsUsableCityYardId("SM"));
    }

    [Fact]
    public void FormatPlace_keeps_spur_token_with_real_city()
    {
        Assert.Equal("FF #Y", LocoRadarDisplay.FormatPlace("#Y", "FF"));
        Assert.Equal("FF", LocoRadarDisplay.FormatPlace(null, "FF"));
        Assert.NotEqual("FF", LocoRadarDisplay.FormatPlace("#Y", "FF"));
    }

    [Fact]
    public void ShortTypeId_strips_Loco_prefix()
    {
        Assert.Equal("DE2", LocoRadarDisplay.ShortTypeId("LocoDE2"));
        Assert.Equal("DE6", LocoRadarDisplay.ShortTypeId("Loco DE6"));
        Assert.Null(LocoRadarDisplay.ShortTypeId(null));
        Assert.Null(LocoRadarDisplay.ShortTypeId("   "));
    }
}
