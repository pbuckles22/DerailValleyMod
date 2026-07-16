using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Joins Monitor HUD segments left-to-right with a fixed separator.
/// </summary>
public static class MonitorHudLine
{
    public const string Separator = "  |  ";

    public static string Join(IEnumerable<string> segments)
    {
        var parts = new List<string>();
        foreach (var segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment))
            {
                parts.Add(segment.Trim());
            }
        }

        return string.Join(Separator, parts);
    }
}
