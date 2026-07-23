using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure park/return mark formatting for the always-on nav chip (1.14).
/// Bearing is the flat-map direction from the player back to the mark.
/// </summary>
public static class ParkMarkDisplay
{
    public const float HereThresholdMeters = 1f;

    public static string FormatCoords(float x, float z) =>
        $"Marked {Round(x)}, {Round(z)}";

    /// <summary>
    /// Return chip, or null when there is no mark (omit from HUD join).
    /// </summary>
    public static string? FormatReturn(float? markX, float? markZ, float? playerX, float? playerZ)
    {
        if (markX is null || markZ is null)
        {
            return null;
        }

        if (playerX is null || playerZ is null)
        {
            return "— Marked";
        }

        var point = TryGetReturnPoint(markX.Value, markZ.Value, playerX.Value, playerZ.Value);
        if (point is null)
        {
            return "— Marked";
        }

        if (point == "here")
        {
            return "Marked here";
        }

        var dx = markX.Value - playerX.Value;
        var dz = markZ.Value - playerZ.Value;
        var meters = (int)Math.Round(Math.Sqrt(dx * dx + dz * dz), MidpointRounding.AwayFromZero);
        return $"Marked {point} {meters}m";
    }

    /// <summary>16-point return bearing, <c>here</c>, or null.</summary>
    public static string? TryGetReturnPoint(float markX, float markZ, float playerX, float playerZ)
    {
        var dx = markX - playerX;
        var dz = markZ - playerZ;
        var distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance < HereThresholdMeters)
        {
            return "here";
        }

        return HeadingDisplay.ToCompassPoint(HeadingDisplay.FromForward(dx, dz));
    }

    private static int Round(float value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
