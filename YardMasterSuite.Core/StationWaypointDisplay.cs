using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure in-zone station waypoint formatting for the always-on nav chip (4.6).
/// HUD shows area id only — bearing/meters live on office AR (declutter / perf).
/// </summary>
public static class StationWaypointDisplay
{
    /// <summary>
    /// Zone waypoint chip, or null when outside a station zone (omit from HUD join).
    /// </summary>
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
        // station/player coords unused for HUD text — kept for API compatibility with callers.
        _ = stationX;
        _ = stationZ;
        _ = playerX;
        _ = playerZ;
        _ = atOffice;
        return "Curr. Area - " + label;
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
