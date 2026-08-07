namespace YardMasterSuite.Core;

/// <summary>
/// When the top loco gadget bar (Speed/Limit/Load/…) may run, and when Limit scan
/// may be allowed to warm without showing that bar (future frame-sliced warm — do not
/// run a full synchronous ScanPostedBoards on look-at; that reintroduces the 0.6.33 hitch).
/// <para>
/// First HUD freeze report (0.6.33 mini-map smoke): look-at loco → T2 power/limit loco +
/// scan=1600m → ~2 s freeze. 0.6.34 only <b>moved</b> that cost to standing/cab entry.
/// </para>
/// </summary>
public static class UsableLocoTrainGate
{
    /// <summary>
    /// True only when the player is in/on a car (<paramref name="hasStandingCar"/>).
    /// Look-at still drives the second inspect bar via <see cref="TargetCarSelection"/>.
    /// </summary>
    public static bool AllowLocoGadgetBar(bool hasStandingCar) => hasStandingCar;

    /// <summary>
    /// True when a non-HUD Limit warm <b>may</b> be scheduled (standing or look-at loco).
    /// Does not imply <see cref="AllowLocoGadgetBar"/>. Callers must not run a full
    /// cold scan synchronously on the look-at path (0.6.36 FAIL).
    /// </summary>
    public static bool AllowLimitScanWarm(bool hasStandingCar, bool lookAtIsLoco) =>
        hasStandingCar || lookAtIsLoco;
}
