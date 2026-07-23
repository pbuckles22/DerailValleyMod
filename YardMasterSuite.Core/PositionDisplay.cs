using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure map-position formatting (1.13). Flat X/Z only (no height).
/// Bundle B.1: no longer shown on the always-on HUD; kept for Tier 2 pos debug.
/// </summary>
public static class PositionDisplay
{
    public static string Format(float? x, float? z)
    {
        if (x is null || z is null)
        {
            return "— Pos";
        }

        return $"Pos {Round(x.Value)}, {Round(z.Value)}";
    }

    private static int Round(float value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
