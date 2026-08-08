using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.4.20.2 Active Roster — dense-yard Limit path must not re-parse every HUD tick.
/// </summary>
public class PostedBoardActiveRosterTests
{
    [Fact]
    public void WithinActiveRadius_keeps_near_drops_far()
    {
        Assert.True(PostedBoardActiveRoster.WithinActiveRadius(0f, 0f, 0f, 0f, 0f, 0f));
        Assert.True(
            PostedBoardActiveRoster.WithinActiveRadius(
                PostedBoardActiveRoster.ActiveRadiusMeters,
                0f,
                0f,
                0f,
                0f,
                0f));
        Assert.False(
            PostedBoardActiveRoster.WithinActiveRadius(
                PostedBoardActiveRoster.ActiveRadiusMeters + 1f,
                0f,
                0f,
                0f,
                0f,
                0f));
    }

    [Fact]
    public void SelectGoverningBehind_picks_closest_behind_along_forward()
    {
        var boards = new[]
        {
            new ParsedPostedBoard(0f, 0f, -50f, 40f),
            new ParsedPostedBoard(0f, 0f, -10f, 60f),
            new ParsedPostedBoard(0f, 0f, 20f, 80f),
        };

        // Loco at origin facing +Z; boards with negative Z are behind.
        var kmh = PostedBoardActiveRoster.SelectGoverningBehindKmh(
            boards,
            locoX: 0f,
            locoY: 0f,
            locoZ: 0f,
            forwardX: 0f,
            forwardY: 0f,
            forwardZ: 1f,
            lookbackMeters: 300f);

        Assert.Equal(60f, kmh);
    }

    [Fact]
    public void SelectGoverningBehind_ignores_ahead_and_beyond_lookback()
    {
        var boards = new[]
        {
            new ParsedPostedBoard(0f, 0f, 50f, 80f),
            new ParsedPostedBoard(0f, 0f, -400f, 40f),
        };

        var kmh = PostedBoardActiveRoster.SelectGoverningBehindKmh(
            boards,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            lookbackMeters: 300f);

        Assert.Null(kmh);
    }
}
