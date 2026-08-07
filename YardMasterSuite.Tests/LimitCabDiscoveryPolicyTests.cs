using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.6.48 FAIL: cab fot=122ms + 8× walk≈60ms with no Align warm in Player.log.
/// </summary>
public class LimitCabDiscoveryPolicyTests
{
    [Fact]
    public void Smoke_048_cab_never_runs_limit_fot()
    {
        Assert.False(LimitCabDiscoveryPolicy.AllowCabLimitFoT);
    }

    [Fact]
    public void Smoke_048_cab_never_cold_getclosest()
    {
        Assert.False(LimitCabDiscoveryPolicy.AllowCabColdTrackAttach);
    }

    [Fact]
    public void Smoke_048_cab_board_walk_requires_align_cache()
    {
        Assert.False(LimitCabDiscoveryPolicy.AllowCabBoardWalk(sessionTrackCacheReady: false));
        Assert.True(LimitCabDiscoveryPolicy.AllowCabBoardWalk(sessionTrackCacheReady: true));
    }
}
