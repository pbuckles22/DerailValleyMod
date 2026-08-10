using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Active Roster — parse-once nearby boards; rare FoT; HUD pick is float-only.
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
    public void NeedsRefresh_first_move_or_empty_retry_never_periodic_when_warm()
    {
        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f,
                lastRefreshAt: -999f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: false));

        // Warm roster: age alone must not re-FoT.
        Assert.False(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.RefreshSeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: false));

        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 11f,
                lastRefreshAt: 10f,
                originX: PostedBoardActiveRoster.MoveInvalidateMeters + 1f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true));

        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.EmptyRetrySeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: true,
                emptyRetriesDone: 0));

        Assert.False(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.EmptyRetrySeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: true,
                emptyRetriesDone: PostedBoardActiveRoster.MaxEmptyRetries));
    }

    [Fact]
    public void PickKmh_single_and_dual()
    {
        var single = new ParsedPostedBoard(
            instanceId: 1,
            x: 0f,
            y: 0f,
            z: 0f,
            forwardX: 0f,
            forwardZ: -1f,
            rightX: 1f,
            rightZ: 0f,
            throughKmh: 60f,
            divergeKmh: 60f,
            isDual: false,
            junctionNearby: false,
            label: "6");
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(single, diverging: true));
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(single, diverging: false));

        var dual = new ParsedPostedBoard(
            2,
            0f,
            0f,
            0f,
            0f,
            -1f,
            1f,
            0f,
            60f,
            40f,
            isDual: true,
            junctionNearby: true,
            label: "6/4");
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(dual, diverging: false));
        Assert.Equal(40f, PostedBoardActiveRoster.PickKmh(dual, diverging: true));
    }

    [Fact]
    public void SelectGoverningBehind_picks_closest_behind_along_forward()
    {
        var boards = new[]
        {
            new ParsedPostedBoard(1, 0f, 0f, -50f, 0f, -1f, 1f, 0f, 40f, 40f, false, false, "4"),
            new ParsedPostedBoard(2, 0f, 0f, -10f, 0f, -1f, 1f, 0f, 60f, 60f, false, false, "6"),
            new ParsedPostedBoard(3, 0f, 0f, 20f, 0f, -1f, 1f, 0f, 80f, 80f, false, false, "8"),
        };

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
            new ParsedPostedBoard(1, 0f, 0f, 50f, 0f, -1f, 1f, 0f, 80f, 80f, false, false, "8"),
            new ParsedPostedBoard(2, 0f, 0f, -400f, 0f, -1f, 1f, 0f, 40f, 40f, false, false, "4"),
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
