using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedLimitFiloTests
{
    [Fact]
    public void PartitionExits_caps_each_side_nearest_first()
    {
        var boards = new[]
        {
            Board(0, 0, 10, 40),
            Board(0, 0, 20, 50),
            Board(0, 0, 30, 60),
            Board(0, 0, 40, 70),
            Board(0, 0, 50, 80),
            Board(0, 0, 60, 90),
            Board(0, 0, -10, 30),
            Board(0, 0, -100, 20),
        };

        PostedLimitFilo.PartitionExits(
            boards,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            out var plus,
            out var minus);

        Assert.Equal(PostedLimitFilo.MaxDepth, plus.Length);
        Assert.Equal(40f, plus[0].ThroughKmh);
        Assert.Equal(80f, plus[4].ThroughKmh);
        Assert.Equal(2, minus.Length);
        Assert.Equal(30f, minus[0].ThroughKmh);
        Assert.Equal(20f, minus[1].ThroughKmh);
    }

    [Fact]
    public void SelectActiveExit_follows_travel_polarity()
    {
        var plus = new[] { Board(1, 0, 10, 60) };
        var minus = new[] { Board(2, 0, -10, 40) };

        var same = PostedLimitFilo.SelectActiveExit(plus, minus, 0f, 1f, 0f, 1f);
        Assert.Same(plus, same);

        var opp = PostedLimitFilo.SelectActiveExit(plus, minus, 0f, 1f, 0f, -1f);
        Assert.Same(minus, opp);
    }

    [Fact]
    public void ShouldLockDirection_above_crawl()
    {
        Assert.False(PostedLimitFilo.ShouldLockDirection(0f));
        Assert.True(PostedLimitFilo.ShouldLockDirection(PostedLimitFilo.DirectionLockMinSpeedKmh + 0.1f));
    }

    private static ParsedPostedBoard Board(int id, float x, float z, float kmh) =>
        new(
            id,
            x,
            0f,
            z,
            0f,
            -1f,
            1f,
            0f,
            kmh,
            kmh,
            false,
            false,
            kmh.ToString("0"));
}
