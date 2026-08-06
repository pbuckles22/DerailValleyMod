namespace YardMasterSuite.Core;

/// <summary>
/// When the top loco gadget bar (Speed/Limit/Load/…) may run.
/// Perf lock (0.6.33 smoke): look-at alone must NOT promote a loco into the cab HUD —
/// that cold-starts a ~1600 m posted-board scan (~2 s freeze).
/// </summary>
public static class UsableLocoTrainGate
{
    /// <summary>
    /// True only when the player is in/on a car (<paramref name="hasStandingCar"/>).
    /// Look-at still drives the second inspect bar via <see cref="TargetCarSelection"/>.
    /// </summary>
    public static bool AllowLocoGadgetBar(bool hasStandingCar) => hasStandingCar;
}
