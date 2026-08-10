using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TurntableTrackResolverTests
{
    [Fact]
    public void PickBest_FailsClosed_OnEmptyOrNull()
    {
        Assert.Null(TurntableTrackResolver.PickBest("SM", null));
        Assert.Null(TurntableTrackResolver.PickBest("SM", Array.Empty<TurntableCandidate>()));
        Assert.Null(TurntableTrackResolver.PickBest("", new[] { new TurntableCandidate("SM-TT", "SM", 10f) }));
        Assert.Null(TurntableTrackResolver.PickBest(null, new[] { new TurntableCandidate("SM-TT", "SM", 10f) }));
    }

    [Fact]
    public void PickBest_FiltersByYardId()
    {
        var candidates = new[]
        {
            new TurntableCandidate("CS-TT", "CS", 50f),
            new TurntableCandidate("MF-TT", "MF", 100f),
        };

        Assert.Equal("MF-TT", TurntableTrackResolver.PickBest("MF", candidates));
        Assert.Equal("CS-TT", TurntableTrackResolver.PickBest("CS", candidates));
        Assert.Null(TurntableTrackResolver.PickBest("SM", candidates));
    }

    [Fact]
    public void PickBest_TieBreaks_ByNearestDistance()
    {
        var candidates = new[]
        {
            new TurntableCandidate("SM-TT-FAR", "SM", 500f),
            new TurntableCandidate("SM-TT-NEAR", "SM", 150f),
            new TurntableCandidate("CS-TT", "CS", 10f),
        };

        Assert.Equal("SM-TT-NEAR", TurntableTrackResolver.PickBest("SM", candidates));
    }

    [Fact]
    public void PickBest_YardMatch_IsCaseInsensitive()
    {
        var candidates = new[] { new TurntableCandidate("mf-tt", "mf", 20f) };
        Assert.Equal("mf-tt", TurntableTrackResolver.PickBest("MF", candidates));
    }

    /// <summary>
    /// Smoke SW: blank TT meta may fall back to nearest — only when player is in that yard.
    /// </summary>
    [Fact]
    public void PickBest_Smoke_SwYardlessTurntable_FallsBackWhenPlayerInYard()
    {
        var candidates = new[]
        {
            new TurntableCandidate("CS-TT", "CS", 9000f),
            new TurntableCandidate("#Y-SW-TT", "", 80f),
            new TurntableCandidate("MF-TT", "MF", 8500f),
        };

        Assert.Equal(
            "#Y-SW-TT",
            TurntableTrackResolver.PickBest(
                "SW",
                candidates,
                nearestFallbackMaxMeters: 500f,
                playerYardId: "SW"));
    }

    [Fact]
    public void PickBest_NoFallback_WhenCityIsNotPlayerYard()
    {
        var candidates = new[]
        {
            new TurntableCandidate("#Y-SW-TT", "", 80f),
        };

        // CME selected while standing in SW — must not steal SW turntable.
        Assert.Null(
            TurntableTrackResolver.PickBest(
                "CME",
                candidates,
                nearestFallbackMaxMeters: 500f,
                playerYardId: "SW"));
    }

    [Fact]
    public void PickBest_PrefersYardMatch_OverNearerFallback()
    {
        var candidates = new[]
        {
            new TurntableCandidate("#Y-NEAR", "", 20f),
            new TurntableCandidate("SW-TT", "SW", 200f),
        };

        Assert.Equal(
            "SW-TT",
            TurntableTrackResolver.PickBest(
                "SW",
                candidates,
                nearestFallbackMaxMeters: 500f,
                playerYardId: "SW"));
    }

    [Fact]
    public void PickBest_Fallback_FailsClosed_WhenNearestBeyondMax()
    {
        var candidates = new[]
        {
            new TurntableCandidate("#Y-FAR", "", 2000f),
        };

        Assert.Null(
            TurntableTrackResolver.PickBest(
                "SW",
                candidates,
                nearestFallbackMaxMeters: 500f,
                playerYardId: "SW"));
    }
}
