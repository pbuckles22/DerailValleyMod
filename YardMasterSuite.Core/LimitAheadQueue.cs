namespace YardMasterSuite.Core;

/// <summary>
/// How many different posted speeds to keep ahead on-route (HUD shows Current + Next only).
/// </summary>
public static class LimitAheadQueue
{
    public const int MaxDepth = 4;

    /// <summary>
    /// True once we have enough ahead boards for a stable Next cushion.
    /// </summary>
    public static bool IsFull(int aheadCount) => aheadCount >= MaxDepth;

    /// <summary>
    /// Stop walking more signs once the ahead cushion is full (FILO window).
    /// </summary>
    public static bool ShouldStopWalk(int governingAheadCount) =>
        governingAheadCount >= MaxDepth;
}

/// <summary>
/// When Limit may reuse last scan vs must walk boards again.
/// Steady ticks should be cache-only (&lt;10 ms).
/// </summary>
public static class LimitScanPolicy
{
    /// <summary>Max meters to coast on estimated Next distance before a refresh walk.</summary>
    public const float MaxCoastMeters = 80f;

    /// <summary>
    /// Prefer cache when we already have a Limit snapshot and have not passed Next / taken a board /
    /// coasted too far without a refresh.
    /// </summary>
    public static bool PreferCache(
        bool hasPersistedSnapshot,
        bool boardTakenThisTick,
        bool junctionChanged,
        float? nextAlongMeters,
        float metersCoastedSinceScan)
    {
        if (!hasPersistedSnapshot || boardTakenThisTick || junctionChanged)
        {
            return false;
        }

        if (nextAlongMeters is float along && along <= 0f)
        {
            return false;
        }

        if (metersCoastedSinceScan >= MaxCoastMeters)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Advance cached Next distance by meters traveled along route (speed integration).
    /// </summary>
    public static float? CoastNextAlong(float? nextAlongMeters, float metersMovedAlong)
    {
        if (nextAlongMeters is not float along)
        {
            return null;
        }

        if (metersMovedAlong <= 0f)
        {
            return along;
        }

        return along - metersMovedAlong;
    }
}
