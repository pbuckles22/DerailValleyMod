using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure loco-type formatting for the local-car HUD bar. Non-locos omit the segment.
/// </summary>
public static class LocoTypeDisplay
{
    public static string? Format(string? typeId)
    {
        var id = typeId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        const string locoPrefix = "Loco";
        if (id!.StartsWith(locoPrefix, StringComparison.OrdinalIgnoreCase)
            && id.Length > locoPrefix.Length)
        {
            id = id.Substring(locoPrefix.Length);
        }

        return string.IsNullOrEmpty(id) ? null : $"Loco {id}";
    }
}

