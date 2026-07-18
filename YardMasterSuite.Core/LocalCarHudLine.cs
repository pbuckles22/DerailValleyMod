using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Single-car HUD bar (look-at preferred; standing fallback when not looking at a car).
/// </summary>
public static class LocalCarHudLine
{
    public static string Format(
        string pipe,
        string handbrake,
        string couplers,
        string carNumber,
        string job,
        string? cargo = null,
        string? locoType = null)
    {
        var parts = new List<string> { pipe, handbrake, couplers, carNumber, job };
        if (!string.IsNullOrEmpty(cargo))
        {
            parts.Add(cargo!);
        }

        if (!string.IsNullOrEmpty(locoType))
        {
            parts.Add(locoType!);
        }

        return MonitorHudLine.Join(parts);
    }
}
