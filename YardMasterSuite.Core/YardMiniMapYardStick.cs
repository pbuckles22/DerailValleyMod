using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Sticky / nested yard pick for desk TT Align + Limit FILO town warm.
/// MFMB is a satellite: only when inside a tight office-radius “fence”, not the job zone / track AABB.
/// Player.log 0.6.26: Station MF→MFMB N still flipped the map — AABB footprint was too early.
/// </summary>
public static class YardMiniMapYardStick
{
    /// <summary>
    /// Temp compound radius from MFMB office (meters). 120 was still early on approach;
    /// smoke @ ~50 m still saw Yard MFMB with Station MF — tighten to 5 while mapping unnamed rails.
    /// </summary>
    public const float SatelliteFenceRadiusMeters = 5f;

    public static bool IsSatelliteYard(string? yardId) =>
        string.Equals(yardId?.Trim(), "MFMB", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when player XZ is within <paramref name="radiusMeters"/> of the satellite office.</summary>
    public static bool IsInsideOfficeFence(
        float playerX,
        float playerZ,
        float officeX,
        float officeZ,
        float radiusMeters = SatelliteFenceRadiusMeters)
    {
        var r = radiusMeters < 0f ? 0f : radiusMeters;
        var dx = playerX - officeX;
        var dz = playerZ - officeZ;
        return dx * dx + dz * dz <= r * r;
    }

    /// <param name="insideFenceSatellites">Satellites whose office-fence currently contains the player.</param>
    public static string? Resolve(
        string? stickyYardId,
        IReadOnlyList<string> inZoneYardIds,
        string? nearestYardId,
        IReadOnlyList<string>? insideFenceSatellites = null)
    {
        if (inZoneYardIds == null || inZoneYardIds.Count == 0)
        {
            return null;
        }

        var sticky = stickyYardId?.Trim();
        var nearest = nearestYardId?.Trim();

        // Inside the compound — only if nearest is also that satellite (Station MFMB),
        // or sticky already on it. Do not steal from Station MF while merely near MB office.
        if (insideFenceSatellites != null && insideFenceSatellites.Count > 0)
        {
            if (!string.IsNullOrEmpty(nearest)
                && IsSatelliteYard(nearest)
                && ContainsYard(insideFenceSatellites, nearest))
            {
                return nearest;
            }

            if (!string.IsNullOrEmpty(sticky)
                && IsSatelliteYard(sticky)
                && ContainsYard(insideFenceSatellites, sticky))
            {
                return sticky;
            }
        }

        // Outside compound: never pick a satellite from job-zone / nearest alone.
        var candidates = new List<string>(inZoneYardIds.Count);
        for (var i = 0; i < inZoneYardIds.Count; i++)
        {
            var id = inZoneYardIds[i]?.Trim();
            if (string.IsNullOrEmpty(id) || IsSatelliteYard(id))
            {
                continue;
            }

            candidates.Add(id!);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(sticky) && !IsSatelliteYard(sticky) && ContainsYard(candidates, sticky))
        {
            return sticky;
        }

        if (!string.IsNullOrEmpty(nearest) && !IsSatelliteYard(nearest) && ContainsYard(candidates, nearest))
        {
            return nearest;
        }

        return candidates[0];
    }

    private static bool ContainsYard(IReadOnlyList<string> yards, string? yardId)
    {
        if (string.IsNullOrEmpty(yardId) || yards == null)
        {
            return false;
        }

        for (var i = 0; i < yards.Count; i++)
        {
            if (string.Equals(yards[i]?.Trim(), yardId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
