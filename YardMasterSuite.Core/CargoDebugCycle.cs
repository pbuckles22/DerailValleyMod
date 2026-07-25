namespace YardMasterSuite.Core;

/// <summary>F7 cargo debug action for look-at consist freight (unload ↔ full load).</summary>
public enum CargoDebugAction
{
    Unload = 0,
    Load = 1,
}

/// <summary>
/// Pure next-action helper for the coupled consist:
/// any freight loaded → unload all; all empty → load all.
/// </summary>
public static class CargoDebugCycle
{
    /// <param name="anyFreightHasCargo">True if any non-loco freight in the trainset has cargo.</param>
    public static CargoDebugAction NextAction(bool anyFreightHasCargo) =>
        anyFreightHasCargo ? CargoDebugAction.Unload : CargoDebugAction.Load;
}
