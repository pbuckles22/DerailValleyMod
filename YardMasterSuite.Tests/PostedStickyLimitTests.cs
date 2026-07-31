using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedStickyLimitTests
{
    [Fact]
    public void Taken_board_sets_sticky()
    {
        Assert.Equal(40f, PostedStickyLimit.Resolve(sticky: 60f, takenKmh: 40f, seedKmh: 60f));
    }

    /// <summary>
    /// 0.5.50 derail: after taking '4 -2.1'=40, a '6'=60 board 273 m behind became the nearest
    /// governing board and raised Limit back to 60 on a downgrade. Sticky must ignore it.
    /// </summary>
    [Fact]
    public void Looser_board_behind_cannot_raise_sticky_without_a_take()
    {
        Assert.Equal(40f, PostedStickyLimit.Resolve(sticky: 40f, takenKmh: null, seedKmh: 60f));
    }

    [Fact]
    public void Nearest_behind_only_seeds_when_sticky_is_unknown()
    {
        Assert.Equal(60f, PostedStickyLimit.Resolve(sticky: null, takenKmh: null, seedKmh: 60f));
        Assert.Null(PostedStickyLimit.Resolve(sticky: null, takenKmh: null, seedKmh: null));
    }

    [Fact]
    public void Take_wins_even_when_it_is_looser_than_sticky()
    {
        // Passing a real 80 board legitimately releases a 40 restriction.
        Assert.Equal(80f, PostedStickyLimit.Resolve(sticky: 40f, takenKmh: 80f, seedKmh: 40f));
    }
}

public class BoardTakeDetectorTests
{
    [Fact]
    public void Ahead_to_behind_is_a_take()
    {
        var detector = new BoardTakeDetector();
        Assert.Null(detector.Observe(1, 40f, alongMeters: 12f));
        Assert.Equal(40f, detector.Observe(1, 40f, alongMeters: -0.5f));
    }

    [Fact]
    public void Staying_behind_is_not_a_repeat_take()
    {
        var detector = new BoardTakeDetector();
        detector.Observe(1, 40f, alongMeters: 8f);
        Assert.Equal(40f, detector.Observe(1, 40f, alongMeters: -1f));
        Assert.Null(detector.Observe(1, 40f, alongMeters: -20f));
    }

    [Fact]
    public void Board_first_seen_behind_is_not_a_take()
    {
        var detector = new BoardTakeDetector();
        Assert.Null(detector.Observe(7, 60f, alongMeters: -40f));
    }

    [Fact]
    public void Reset_forgets_sides()
    {
        var detector = new BoardTakeDetector();
        detector.Observe(1, 40f, alongMeters: 10f);
        detector.Reset();
        Assert.Null(detector.Observe(1, 40f, alongMeters: -1f));
    }
}
