namespace YardMasterSuite.Core;

/// <summary>
/// Diagnostic / QoL gate: when false, Monitor/AR <c>OnGUI</c> must early-return (no layout/paint),
/// while <c>Update</c> telemetry keeps running. Port by copying this type + one early-return per OnGUI.
/// Hotkey / settings callers flip <see cref="DrawVisuals"/>.
/// </summary>
public static class HudDrawGate
{
    /// <summary>Default on — normal play draws HUD.</summary>
    public static bool DrawVisuals { get; set; } = true;

    public static void Toggle() => DrawVisuals = !DrawVisuals;

    /// <summary>Reset between smokes / tests so state does not leak.</summary>
    public static void ResetForTests() => DrawVisuals = true;
}
