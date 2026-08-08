namespace YardMasterSuite.Core;

/// <summary>
/// Bottom HUD legend for Tier 2 debug hotkeys (visible only while debug gate is on).
/// </summary>
public static class DebugHotkeyHudLine
{
    /// <summary>One bar listing cycle keys (same chip style as other Monitor bars).</summary>
    public static string Format() =>
        "Shift+F1 Debug | Shift+F2 Logs | F5 DH4/DE6 | F6 Load | F7 Cargo | F8 Fluids | F9 Couplers "
        + "| F11 Licenses";
}
