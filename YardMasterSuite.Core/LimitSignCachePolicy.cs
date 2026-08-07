namespace YardMasterSuite.Core;

/// <summary>
/// When Limit may call FindObjectsOfType&lt;SignDebug&gt;.
/// 0.6.44 smoke: session board-cache already attached 31 signs, cab still paid fot=119ms.
/// </summary>
public static class LimitSignCachePolicy
{
    /// <summary>Cold / unwarmed: same ballpark as legacy SignDebug refresh.</summary>
    public const float ColdRefreshSeconds = 10f;

    /// <summary>Warmed session: rare FoT only (streaming new boards).</summary>
    public const float WarmRefreshSeconds = 60f;

    /// <summary>
    /// True when a FoT (or forced refresh) should run.
    /// Skip when track cache is ready and we already hold signs, until warm interval elapses.
    /// </summary>
    public static bool ShouldRunFoT(
        bool sessionTrackCacheReady,
        int cachedSignCount,
        float secondsSinceCacheAdopt)
    {
        if (cachedSignCount <= 0)
        {
            return true;
        }

        if (!sessionTrackCacheReady)
        {
            return secondsSinceCacheAdopt >= ColdRefreshSeconds;
        }

        return secondsSinceCacheAdopt >= WarmRefreshSeconds;
    }
}
