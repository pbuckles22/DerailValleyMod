using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.6.33+ cab hitch: stage Limit work across HUD ticks (paint → FoT → board walk).
/// </summary>
public class LimitScanStartupTests
{
    [Fact]
    public void Smoke_033_first_usable_ticks_defer_all_limit_heavy_work()
    {
        Assert.False(LimitScanStartup.AllowSignCacheRefresh(0));
        Assert.False(LimitScanStartup.AllowSignCacheRefresh(1));
        Assert.False(LimitScanStartup.AllowBoardWalk(0));
        Assert.False(LimitScanStartup.AllowBoardWalk(1));
    }

    [Fact]
    public void FoT_tick_allows_sign_refresh_but_not_board_walk()
    {
        Assert.True(LimitScanStartup.AllowSignCacheRefresh(2));
        Assert.False(LimitScanStartup.AllowBoardWalk(2));
        Assert.False(LimitScanStartup.AllowHeavyScan(2));
    }

    [Fact]
    public void Board_walk_requires_cache_even_after_wait_budget()
    {
        Assert.False(LimitScanStartup.AllowBoardWalkWithCache(3, boardTrackCacheReady: false));
        Assert.True(LimitScanStartup.AllowBoardWalkWithCache(3, boardTrackCacheReady: true));
        Assert.False(LimitScanStartup.AllowBoardWalkWithCache(
            LimitScanStartup.DeferHudTicks
            + LimitScanStartup.BoardWalkExtraTicks
            + LimitScanStartup.BoardCacheWaitExtraTicks,
            boardTrackCacheReady: false));
    }
}
