namespace YardMasterSuite.Core;

/// <summary>
/// Always-on personal nav bar (version · Heading · Marked · Station).
/// Bundle B.1: Pos chip removed from this line (keep Heading until Bundle A).
/// </summary>
public static class AlwaysOnHudLine
{
    public static string Format(
        string versionChip,
        string heading,
        string? park = null,
        string? station = null) =>
        MonitorHudLine.Join(new[] { versionChip, heading, park ?? "", station ?? "" });
}
