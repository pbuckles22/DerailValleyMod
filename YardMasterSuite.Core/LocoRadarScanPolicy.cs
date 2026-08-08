namespace YardMasterSuite.Core;

/// <summary>
/// When the other-loco AR radar (4.10) may run its scene-wide loco scan.
/// Periodic rescans (1 s, then 3 s) produced a rhythmic driving hitch even inside the town
/// boundary (0.6.50–0.6.51). Product lock: one FoT on town entry, then keep the set until
/// the player leaves town. Captions read live car transforms, so distances stay fresh.
/// </summary>
public static class LocoRadarScanPolicy
{
    /// <summary>Town boundary measured from station center (m). First pass — retune in smoke.</summary>
    public const float TownBoundaryRadiusMeters = 400f;

    public static bool IsInsideTown(
        float sqrDistanceFromStationCenter,
        float townRadiusMeters = TownBoundaryRadiusMeters)
    {
        if (float.IsNaN(sqrDistanceFromStationCenter) || sqrDistanceFromStationCenter < 0f)
        {
            return false;
        }

        var radius = townRadiusMeters < 0f ? 0f : townRadiusMeters;
        return sqrDistanceFromStationCenter <= radius * radius;
    }

    /// <summary>
    /// True only on the rising edge of a town visit (or after an explicit invalidate).
    /// Never true again until the player leaves town and re-enters.
    /// </summary>
    public static bool ShouldScan(
        bool optionEnabled,
        bool insideTown,
        bool alreadyScannedThisVisit) =>
        optionEnabled && insideTown && !alreadyScannedThisVisit;
}
