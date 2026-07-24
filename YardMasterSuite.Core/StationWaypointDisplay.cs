using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure in-zone station waypoint formatting for the always-on nav chip (4.6 / Bundle C).
/// Bearing/distance are flat-map from the player to the station office.
/// <c>here</c> uses the same office gate as AR house hide (<see cref="ArProximityHide.IsAtOffice"/>).
/// </summary>
public static class StationWaypointDisplay
{
    /// <summary>
    /// Zone waypoint chip, or null when outside a station zone (omit from HUD join).
    /// </summary>
    /// <param name="atOffice">
    /// True when player is inside the office footprint / radius — same predicate as house AR hide.
    /// </param>
    public static string? Format(
        bool inZone,
        string? yardId,
        float? stationX,
        float? stationZ,
        float? playerX,
        float? playerZ,
        bool atOffice = false)
    {
        if (!inZone)
        {
            return null;
        }

        var label = string.IsNullOrWhiteSpace(yardId) ? "—" : yardId!.Trim();
        if (stationX is null || stationZ is null)
        {
            return "— Station";
        }

        if (playerX is null || playerZ is null)
        {
            return "— Station";
        }

        var point = TryGetWalkPoint(stationX.Value, stationZ.Value, playerX.Value, playerZ.Value, atOffice);
        if (point is null)
        {
            return "— Station";
        }

        if (point == "here")
        {
            return $"Station {label} here";
        }

        var dx = stationX.Value - playerX.Value;
        var dz = stationZ.Value - playerZ.Value;
        var meters = (int)Math.Round(Math.Sqrt(dx * dx + dz * dz), MidpointRounding.AwayFromZero);
        return $"Station {label} {point} {meters}m";
    }

    /// <summary>16-point walk bearing toward station, <c>here</c> when <paramref name="atOffice"/>, or null.</summary>
    public static string? TryGetWalkPoint(
        float stationX,
        float stationZ,
        float playerX,
        float playerZ,
        bool atOffice = false)
    {
        if (atOffice)
        {
            return "here";
        }

        var dx = stationX - playerX;
        var dz = stationZ - playerZ;
        return HeadingDisplay.ToCompassPoint(HeadingDisplay.FromForward(dx, dz));
    }
}
