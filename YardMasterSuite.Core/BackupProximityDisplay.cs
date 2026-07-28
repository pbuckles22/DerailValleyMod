using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure backup-proximity formatting for long-train reverse shunting (4.11).
/// Display meters to one tenth. Color: green ≤0.5 m when in couple scan
/// (brake window); yellow above that through 30.0 m; plain beyond. No "Couple ready".
/// </summary>
public static class BackupProximityDisplay
{
    /// <summary>Match game <c>Coupler.COUPLING_SCAN_RANGE</c> (1.5 m) for near detection.</summary>
    public const float CoupleNearRangeMeters = 1.5f;

    /// <summary>Green at or below this display value when in couple scan (smoke: 0.5 close).</summary>
    public const float GreenMaxDisplayMeters = 0.5f;

    /// <summary>Yellow caution through this display value (above green) — long/heavy brake window.</summary>
    public const float CautionMaxDisplayMeters = 30f;

    /// <summary>Beyond this, treat as unknown (show <c>Rear —</c>).</summary>
    public const float MaxDisplayMeters = 80f;

    public const string NearColor = "#55FF55";
    public const string CautionColor = "#FFCC00";

    /// <summary>
    /// Format HUD fragment. Empty string = omit chip (no free tip).
    /// <paramref name="inCoupleRange"/> required for green; also fills unknown scan as 0.0 m.
    /// <paramref name="label"/> is <c>Rear</c> or <c>Front</c> (4.12).
    /// </summary>
    public static string Format(
        float? clearanceMeters,
        bool inCoupleRange,
        bool tipActive = true,
        string label = "Rear") =>
        FormatCore(clearanceMeters, inCoupleRange, tipActive, richText: false, label);

    public static string FormatHud(
        float? clearanceMeters,
        bool inCoupleRange,
        bool tipActive = true,
        string label = "Rear") =>
        FormatCore(clearanceMeters, inCoupleRange, tipActive, richText: true, label);

    /// <summary>True when distance is within game couple-scan band.</summary>
    public static bool IsInCoupleRange(float? clearanceMeters, float rangeMeters = CoupleNearRangeMeters)
    {
        if (clearanceMeters is null || float.IsNaN(clearanceMeters.Value))
        {
            return false;
        }

        var m = clearanceMeters.Value;
        return m >= 0f && m <= rangeMeters;
    }

    /// <summary>Clamp / round to tenths; null when out of useful range or unknown.</summary>
    public static float? NormalizeClearance(float? meters, float maxDisplayMeters = MaxDisplayMeters)
    {
        if (meters is null || float.IsNaN(meters.Value) || meters.Value < 0f)
        {
            return null;
        }

        if (meters.Value > maxDisplayMeters)
        {
            return null;
        }

        return (float)Math.Round(meters.Value, 1, MidpointRounding.AwayFromZero);
    }

    private static string FormatCore(
        float? clearanceMeters,
        bool inCoupleRange,
        bool tipActive,
        bool richText,
        string label)
    {
        if (!tipActive)
        {
            return string.Empty;
        }

        var end = string.IsNullOrEmpty(label) ? "Rear" : label;

        var m = NormalizeClearance(clearanceMeters);
        if (m is null && inCoupleRange)
        {
            m = 0f;
        }

        if (m is null)
        {
            return $"{end} —";
        }

        var text = $"{end} {m:0.0}m";
        if (!richText)
        {
            return text;
        }

        // Green when scan says couple-near AND display ≤0.5 m (brake window).
        if (inCoupleRange && m <= GreenMaxDisplayMeters)
        {
            return $"<color={NearColor}>{text}</color>";
        }

        if (m <= CautionMaxDisplayMeters)
        {
            return $"<color={CautionColor}>{text}</color>";
        }

        return text;
    }
}
