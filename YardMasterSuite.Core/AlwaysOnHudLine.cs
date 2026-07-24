namespace YardMasterSuite.Core;

/// <summary>
/// Always-on personal nav bar (Heading · Marked · Station).
/// Ship version lives in UMM Mod Manager / info.json — not on the HUD.
/// </summary>
public static class AlwaysOnHudLine
{
    public static string Format(
        string heading,
        string? park = null,
        string? station = null) =>
        MonitorHudLine.Join(new[] { heading, park ?? "", station ?? "" });
}
