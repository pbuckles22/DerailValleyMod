using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure meters-to-Regular-destroy-edge for Bundle D preview/prep HUD.
/// Taken jobs never use this chip (no distance on validated jobs).
/// Includes a flat consist safety buffer so the HUD hits zero before the loco nose
/// crosses the wipe line (game still keys off player/cab position).
/// </summary>
public static class PreviewEdgeDisplay
{
    public const float WarningMeters = 200f;
    public const float CriticalMeters = 50f;
    /// <summary>~DE6-length cushion so Preview reaches OUT before the consist nose does.</summary>
    public const float SafetyBufferMeters = 30f;
    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";
    public const string Label = "Preview";

    /// <summary>
    /// Boundary radius minus player distance from station center, minus
    /// <see cref="SafetyBufferMeters"/> (negative = treat as outside / OUT).
    /// </summary>
    public static float? MetersRemaining(float? playerDistanceFromCenter, float? zoneRadiusMeters)
    {
        if (playerDistanceFromCenter is null || zoneRadiusMeters is null || zoneRadiusMeters.Value <= 0f)
        {
            return null;
        }

        return (zoneRadiusMeters.Value - playerDistanceFromCenter.Value) - SafetyBufferMeters;
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
            return $"— {Label}";
        }

        string text;
        if (metersRemaining.Value < 0f)
        {
            text = $"{Label} OUT";
        }
        else
        {
            var meters = (int)Math.Round(metersRemaining.Value, MidpointRounding.AwayFromZero);
            text = $"{Label} {meters}m";
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
