using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure meters-to-zone-edge formatting for the Active Job HUD (4.8).
/// </summary>
public static class ZoneEdgeDisplay
{
    public const float WarningMeters = 200f;
    public const float CriticalMeters = 50f;
    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";

    /// <summary>Zone radius minus player distance from station center (negative = outside).</summary>
    public static float? MetersRemaining(float? playerDistanceFromCenter, float? zoneRadiusMeters)
    {
        if (playerDistanceFromCenter is null || zoneRadiusMeters is null || zoneRadiusMeters.Value <= 0f)
        {
            return null;
        }

        return zoneRadiusMeters.Value - playerDistanceFromCenter.Value;
    }

    public static float? RadiusFromSqr(float? zoneRadiusSquared)
    {
        if (zoneRadiusSquared is null || zoneRadiusSquared.Value <= 0f)
        {
            return null;
        }

        return (float)Math.Sqrt(zoneRadiusSquared.Value);
    }

    public static float? DistanceFromSqr(float? playerDistanceSquared)
    {
        if (playerDistanceSquared is null || playerDistanceSquared.Value < 0f)
        {
            return null;
        }

        return (float)Math.Sqrt(playerDistanceSquared.Value);
    }

    public static string Format(float? metersRemaining, bool richText = false)
    {
        if (metersRemaining is null)
        {
            return "— Zone";
        }

        string text;
        if (metersRemaining.Value < 0f)
        {
            text = "Zone OUT";
        }
        else
        {
            var meters = (int)Math.Round(metersRemaining.Value, MidpointRounding.AwayFromZero);
            text = $"Zone {meters}m";
        }

        if (!richText)
        {
            return text;
        }

        if (metersRemaining.Value < CriticalMeters)
        {
            return $"<color={CriticalColor}>{text}</color>";
        }

        if (metersRemaining.Value < WarningMeters)
        {
            return $"<color={WarningColor}>{text}</color>";
        }

        return text;
    }
}
