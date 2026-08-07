using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LimitAheadQueueTests
{
    [Fact]
    public void Stop_walk_at_four_governing_ahead_boards()
    {
        Assert.False(LimitAheadQueue.ShouldStopWalk(3));
        Assert.True(LimitAheadQueue.ShouldStopWalk(4));
        Assert.True(LimitAheadQueue.IsFull(4));
    }
}

public class LimitScanPolicyTests
{
    [Fact]
    public void Prefer_cache_when_stable_with_next_ahead()
    {
        Assert.True(LimitScanPolicy.PreferCache(
            hasPersistedSnapshot: true,
            boardTakenThisTick: false,
            junctionChanged: false,
            nextAlongMeters: 400f,
            metersCoastedSinceScan: 10f));
    }

    [Fact]
    public void Rescan_when_board_taken_or_next_passed_or_coasted_too_far()
    {
        Assert.False(LimitScanPolicy.PreferCache(
            true, boardTakenThisTick: true, false, 400f, 0f));
        Assert.False(LimitScanPolicy.PreferCache(
            true, false, false, nextAlongMeters: -1f, 0f));
        Assert.False(LimitScanPolicy.PreferCache(
            true, false, false, 400f, LimitScanPolicy.MaxCoastMeters));
    }

    [Fact]
    public void Coast_next_along_subtracts_meters_moved()
    {
        Assert.Equal(350f, LimitScanPolicy.CoastNextAlong(400f, 50f));
        Assert.Null(LimitScanPolicy.CoastNextAlong(null, 50f));
    }
}
