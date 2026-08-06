using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Single-car HUD bar (look-at preferred; standing fallback when not looking at a car).
/// No Pipe / Car # / cargo-type — declutter + perf.
/// </summary>
public static class LocalCarHudLine
{
    public static string Format(
        string handbrake,
        string couplers,
        string? job,
        string? track,
        string? identityChip)
    {
        var parts = new List<string>
        {
            handbrake, couplers,
        };
        if (!string.IsNullOrWhiteSpace(job))
        {
            parts.Add(job!.Trim());
        }

        if (!string.IsNullOrWhiteSpace(track))
        {
            parts.Add(track!.Trim());
        }

        if (!string.IsNullOrEmpty(identityChip))
        {
            parts.Add(identityChip!);
        }

        return MonitorHudLine.Join(parts);
    }
}
