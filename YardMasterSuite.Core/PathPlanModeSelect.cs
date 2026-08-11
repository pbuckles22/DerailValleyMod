using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pick World vs Yard PathPlan profile for a single Align / Compute leg.
/// Multi-leg composition (World then Yard) is Switch List / 3.7 — not this helper.
/// </summary>
public static class PathPlanModeSelect
{
    /// <summary>
    /// Yard when origin and effective dest share a city yard (incl. anonymous TT with session yard).
    /// Otherwise World.
    /// </summary>
    public static PathPlanMode ForTrip(
        string? originTrackId,
        string? destTrackId,
        string? destYardOverride = null,
        Func<string, string?>? yardFor = null)
    {
        yardFor ??= PathRouteConstraints.YardIdOf;
        var origin = originTrackId?.Trim();
        var dest = destTrackId?.Trim();
        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(dest))
        {
            return PathPlanMode.World;
        }

        var originYard = yardFor(origin!) ?? PathRouteConstraints.YardIdOf(origin);
        var destYard = PathRouteConstraints.EffectiveDestYardId(dest, destYardOverride, yardFor);
        if (originYard != null
            && destYard != null
            && string.Equals(originYard, destYard, StringComparison.OrdinalIgnoreCase))
        {
            return PathPlanMode.Yard;
        }

        return PathPlanMode.World;
    }
}
