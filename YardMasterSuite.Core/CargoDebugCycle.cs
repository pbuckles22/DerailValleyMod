namespace YardMasterSuite.Core;

/// <summary>F7 cargo debug action for look-at freight (unload ↔ full load).</summary>
public enum CargoDebugAction
{
    Unload = 0,
    Load = 1,
}

/// <summary>Pure next-action helper: loaded → unload; empty → load.</summary>
public static class CargoDebugCycle
{
    public static CargoDebugAction NextAction(bool hasCargo) =>
        hasCargo ? CargoDebugAction.Unload : CargoDebugAction.Load;
}
