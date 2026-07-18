using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure grade formatting. Slope ratio is rise/run (m/m); display as percent with sign.
/// </summary>
public static class GradeDisplay
{
    /// <summary>
    /// Grade percent from a direction vector (e.g. car forward). Positive = climbing.
    /// </summary>
    public static float PercentFromDirection(float x, float y, float z)
    {
        var horizontal = Math.Sqrt((x * x) + (z * z));
        if (horizontal < 1e-6)
        {
            return 0f;
        }

        return (float)(y / horizontal * 100.0);
    }

    public static string FormatPercent(float? gradePercent) =>
        gradePercent is null
            ? "— Grade"
            : $"Grade {FormatSigned(gradePercent.Value)} %";

    private static string FormatSigned(float value)
    {
        var rounded = (float)Math.Round(value, 1, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded) < 0.05f)
        {
            return "0.0";
        }

        return rounded > 0f ? $"+{rounded:0.0}" : $"{rounded:0.0}";
    }
}
