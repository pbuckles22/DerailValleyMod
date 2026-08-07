using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.6.44 smoke: board-cache warm finished attached=31, then cab still fot=119ms.
/// Skip FoT when session track cache is ready and signs are already adopted.
/// </summary>
public class LimitSignCachePolicyTests
{
    [Fact]
    public void Smoke_044_skip_cab_fot_when_session_cache_warm_with_signs()
    {
        Assert.False(LimitSignCachePolicy.ShouldRunFoT(
            sessionTrackCacheReady: true,
            cachedSignCount: 31,
            secondsSinceCacheAdopt: 5f));
    }

    [Fact]
    public void Run_fot_when_no_signs_or_cache_cold()
    {
        Assert.True(LimitSignCachePolicy.ShouldRunFoT(
            sessionTrackCacheReady: false,
            cachedSignCount: 0,
            secondsSinceCacheAdopt: 0f));
        Assert.True(LimitSignCachePolicy.ShouldRunFoT(
            sessionTrackCacheReady: false,
            cachedSignCount: 31,
            secondsSinceCacheAdopt: 10f));
    }

    [Fact]
    public void Warm_cache_still_refreshes_rarely_for_streaming()
    {
        Assert.True(LimitSignCachePolicy.ShouldRunFoT(
            sessionTrackCacheReady: true,
            cachedSignCount: 31,
            secondsSinceCacheAdopt: LimitSignCachePolicy.WarmRefreshSeconds));
    }
}
