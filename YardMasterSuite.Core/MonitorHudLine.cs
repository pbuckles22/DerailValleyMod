using System.Collections.Generic;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// Joins Monitor HUD segments left-to-right with a fixed separator.
/// Runs at the 10 Hz label rebuild, so it reuses one builder instead of a per-call list + trims.
/// </summary>
public static class MonitorHudLine
{
    public const string Separator = "  |  ";

    [System.ThreadStatic]
    private static StringBuilder? _builder;

    public static string Join(IEnumerable<string> segments)
    {
        var builder = _builder ??= new StringBuilder(256);
        builder.Length = 0;

        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            var start = 0;
            var end = segment.Length - 1;
            while (start <= end && char.IsWhiteSpace(segment[start]))
            {
                start++;
            }

            while (end >= start && char.IsWhiteSpace(segment[end]))
            {
                end--;
            }

            if (builder.Length > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(segment, start, end - start + 1);
        }

        return builder.ToString();
    }
}
