using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Fail-closed Align when rem is absurd for a same-yard dest (smoke: 10 km Align while in SW).
/// </summary>
public static class AlignLocalRemGuard
{
    /// <summary>Same-yard trip should not need a multi-kilometer corridor.</summary>
    public const float DefaultMaxSameYardMeters = 3000f;

    /// <summary>
    /// True when dest yard equals player yard but remaining meters exceed the local max.
    /// </summary>
    public static bool IsImplausibleSameYardTrip(
        string? destYardId,
        string? playerYardId,
        float? remainingMeters,
        float maxSameYardMeters = DefaultMaxSameYardMeters)
    {
        if (remainingMeters == null
            || remainingMeters.Value <= 0f
            || maxSameYardMeters <= 0f
            || string.IsNullOrWhiteSpace(destYardId)
            || string.IsNullOrWhiteSpace(playerYardId))
        {
            return false;
        }

        var dest = destYardId!.Trim();
        var player = playerYardId!.Trim();
        if (!string.Equals(dest, player, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return remainingMeters.Value > maxSameYardMeters;
    }
}
