using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure in-zone station waypoint formatting for the always-on nav chip (4.6).
/// Bearing/distance are flat-map from the player to the station office.
/// Bundle B.2: map coords dropped from the chip (id + distance / here only).
/// </summary>
public static class StationWaypointDisplay
{
    public const float HereThresholdMeters = 1f;

    /// <summary>
    /// Zone waypoint chip, or null when outside a station zone (omit from HUD join).
    /// </summary>
    public static string? Format(
        bool inZone,
        string? yardId,
        float? stationX,
        float? stationZ,
        float? playerX,
        float? playerZ)
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

        var point = TryGetWalkPoint(stationX.Value, stationZ.Value, playerX.Value, playerZ.Value);
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

    /// <summary>16-point walk bearing toward station, <c>here</c>, or null.</summary>
    public static string? TryGetWalkPoint(float stationX, float stationZ, float playerX, float playerZ)
    {
        var dx = stationX - playerX;
        var dz = stationZ - playerZ;
        var distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance < HereThresholdMeters)
        {
            return "here";
        }

        return HeadingDisplay.ToCompassPoint(HeadingDisplay.FromForward(dx, dz));
    }
}
