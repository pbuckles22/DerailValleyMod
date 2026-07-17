using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure handbrake count helpers for Monitor HUD.
/// </summary>
public static class HandbrakeDisplay
{
    /// <summary>Positions at or below this are treated as released.</summary>
    public const float AppliedThreshold = 0.01f;

    public static bool IsApplied(float handbrakePosition) =>
        handbrakePosition > AppliedThreshold;

    public static int CountApplied(IEnumerable<float> handbrakePositions)
    {
        var count = 0;
        foreach (var position in handbrakePositions)
        {
            if (IsApplied(position))
            {
                count++;
            }
        }

        return count;
    }

    public static string FormatCount(int? appliedCount) =>
        appliedCount is null ? "— HB" : $"{appliedCount.Value} HB";
}
