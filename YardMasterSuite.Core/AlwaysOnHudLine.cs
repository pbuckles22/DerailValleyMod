namespace YardMasterSuite.Core;

/// <summary>
/// Always-on personal nav bar (Heading · Marked · Station · Path · Clock).
/// Ship version lives in UMM Mod Manager / info.json — not on the HUD.
/// </summary>
public static class AlwaysOnHudLine
{
    public static string Format(
        string heading,
        string? park = null,
        string? station = null,
        string? path = null,
        string? facing = null,
        string? clock = null) =>
        MonitorHudLine.Join(new[]
        {
            heading,
            park ?? "",
            station ?? "",
            path ?? "",
            facing ?? "",
            clock ?? "",
        });
}
