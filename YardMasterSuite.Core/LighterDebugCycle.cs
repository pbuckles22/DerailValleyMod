namespace YardMasterSuite.Core;

/// <summary>
/// F12 lighter inventory debug cycle for steam fire-up smoke.
/// </summary>
public enum LighterDebugPhase
{
    Real = 0,
    InInventory = 1,
    Removed = 2,
}

/// <summary>Pure next-phase helper for lighter debug cycle.</summary>
public static class LighterDebugCycle
{
    public static LighterDebugPhase Next(LighterDebugPhase current) =>
        current switch
        {
            LighterDebugPhase.Real => LighterDebugPhase.InInventory,
            LighterDebugPhase.InInventory => LighterDebugPhase.Removed,
            _ => LighterDebugPhase.Real,
        };
}
