namespace YardMasterSuite.Core;

/// <summary>
/// Session gate for Tier 2 debug hotkeys (Shift+F1). Default on for smoke sessions.
/// </summary>
public static class DebugHotkeyGate
{
    public static bool Enabled { get; private set; } = true;

    public static bool Toggle()
    {
        Enabled = !Enabled;
        return Enabled;
    }

    public static void SetEnabled(bool enabled) => Enabled = enabled;
}
