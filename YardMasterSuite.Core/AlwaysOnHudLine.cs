namespace YardMasterSuite.Core;

/// <summary>
/// Always-on personal nav bar (Version · Heading · Marked · Station · Path · Clock).
/// Version chip is in-world only (caller must gate on world session).
/// </summary>
public static class AlwaysOnHudLine
{
    public static string Format(
        string heading,
        string? park = null,
        string? station = null,
        string? path = null,
        string? facing = null,
        string? clock = null,
        string? version = null) =>
        MonitorHudLine.Join(new[]
        {
            version ?? "",
            heading,
            park ?? "",
            station ?? "",
            path ?? "",
            facing ?? "",
            clock ?? "",
        });
}
