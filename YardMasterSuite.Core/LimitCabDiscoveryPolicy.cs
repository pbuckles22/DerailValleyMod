namespace YardMasterSuite.Core;

/// <summary>
/// 0.6.48 smoke FAIL: cab still paid fot≈120ms + 8× GetClosest≈60ms when Align never warmed.
/// Cab Limit must never discover boards via FoT / world GetClosest — Align owns that cost.
/// </summary>
public static class LimitCabDiscoveryPolicy
{
    /// <summary>
    /// Cab Limit path never runs FindObjectsOfType&lt;SignDebug&gt;.
    /// Align / Set dest <c>WarmForPlan</c> owns FoT + path-local attach.
    /// </summary>
    public static bool AllowCabLimitFoT => false;

    /// <summary>
    /// Cab Limit path never cold-attaches boards (RailTrack.GetClosest).
    /// Only session track cache from Align warm may answer track queries.
    /// </summary>
    public static bool AllowCabColdTrackAttach => false;

    /// <summary>
    /// Board walk may run only when Align (or prior warm) left track attaches.
    /// Exhausting a wait budget must not unlock a cold FoT walk.
    /// </summary>
    public static bool AllowCabBoardWalk(bool sessionTrackCacheReady) =>
        sessionTrackCacheReady;
}
