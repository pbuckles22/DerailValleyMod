using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LimitDiscoveryPaceTests
{
    [Fact]
    public void Cab_entry_burst_is_one_sign_then_pause()
    {
        Assert.True(LimitDiscoveryPace.ContinueBurst(0));
        Assert.False(LimitDiscoveryPace.ContinueBurst(1));
        Assert.False(LimitDiscoveryPace.AllowBurst(0.05f));
        Assert.True(LimitDiscoveryPace.AllowBurst(0.1f));
    }

    [Fact]
    public void Warm_enough_stops_before_full_fot_dump()
    {
        Assert.False(LimitDiscoveryPace.IsWarmEnough(
            hasBehindCurrent: false, aheadGoverningCount: 0, evaluatedThisWarm: 3));
        Assert.True(LimitDiscoveryPace.IsWarmEnough(
            hasBehindCurrent: true, aheadGoverningCount: 1, evaluatedThisWarm: 4));
        Assert.True(LimitDiscoveryPace.IsWarmEnough(
            hasBehindCurrent: false, aheadGoverningCount: 4, evaluatedThisWarm: 4));
        Assert.True(LimitDiscoveryPace.IsWarmEnough(
            hasBehindCurrent: false, aheadGoverningCount: 0, evaluatedThisWarm: 8));
    }
}
