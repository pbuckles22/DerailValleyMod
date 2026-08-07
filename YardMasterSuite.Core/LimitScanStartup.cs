namespace YardMasterSuite.Core;

/// <summary>
/// Cab-entry hitch lock: heavy posted-board work must not run on the first HUD tick(s)
/// after a consist becomes usable. Paint Fuel/Speed/… with <c>— Limit</c> first.
/// FoT SignDebug is its own deferred tick; board walk waits for session board-cache when possible.
/// </summary>
public static class LimitScanStartup
{
    /// <summary>HUD refreshes (~0.1 s) before any Limit heavy work.</summary>
    public const int DeferHudTicks = 2;

    /// <summary>Extra tick after defer before FoT (board walk may wait longer for cache).</summary>
    public const int BoardWalkExtraTicks = 1;

    /// <summary>
    /// Legacy wait ticks after FoT (kept for docs/tests). Exhaustion must NOT unlock cold cab walk —
    /// see <see cref="LimitCabDiscoveryPolicy"/>.
    /// </summary>
    public const int BoardCacheWaitExtraTicks = 20;

    public static bool AllowSignCacheRefresh(int usableHudTicksSinceBoard) =>
        usableHudTicksSinceBoard >= DeferHudTicks;

    public static bool AllowBoardWalk(int usableHudTicksSinceBoard) =>
        usableHudTicksSinceBoard >= DeferHudTicks + BoardWalkExtraTicks;

    /// <summary>
    /// Board walk only when paced startup allows AND Align (or prior warm) left track cache.
    /// Waiting longer never unlocks FoT/GetClosest discovery in cab (0.6.48 FAIL).
    /// </summary>
    public static bool AllowBoardWalkWithCache(
        int usableHudTicksSinceBoard,
        bool boardTrackCacheReady) =>
        AllowBoardWalk(usableHudTicksSinceBoard)
        && LimitCabDiscoveryPolicy.AllowCabBoardWalk(boardTrackCacheReady);

    /// <summary>True once board walk is allowed (full ScanPostedBoards).</summary>
    public static bool AllowHeavyScan(int usableHudTicksSinceBoard) =>
        AllowBoardWalk(usableHudTicksSinceBoard);
}
