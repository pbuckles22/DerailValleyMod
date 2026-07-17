using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure brake-pipe formatting. Game pressure is bar (DV native).
/// </summary>
public static class BrakePipeDisplay
{
    public static string FormatBar(float? pressureBar) =>
        pressureBar is null
            ? "— Pipe"
            : $"Pipe {Math.Round(pressureBar.Value, 1, MidpointRounding.AwayFromZero):0.0} bar";
}
