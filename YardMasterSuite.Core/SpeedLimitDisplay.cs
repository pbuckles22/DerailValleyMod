using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure speed-limit formatting for the train HUD bar (geometry / board km/h).
/// Yellow within <see cref="NearThresholdKmh"/> of the limit; red when over.
/// </summary>
public static class SpeedLimitDisplay
{
    /// <summary>Yellow band starts this many km/h below the posted limit (inclusive at limit).</summary>
    public const float NearThresholdKmh = 5f;

    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";

    public static string Format(float? limitKmh) =>
        FormatCore(limitKmh, richText: false, severity: LimitSeverity.None);

    public static string FormatHud(float? speedKmh, float? limitKmh) =>
        FormatCore(limitKmh, richText: true, severity: Severity(speedKmh, limitKmh));

    public static LimitSeverity Severity(float? speedKmh, float? limitKmh)
    {
        if (speedKmh is null || limitKmh is null)
        {
            return LimitSeverity.None;
        }

        var speed = Round(speedKmh.Value);
        var limit = Round(limitKmh.Value);
        if (speed > limit)
        {
            return LimitSeverity.Over;
        }

        if (speed > limit - NearThresholdKmh)
        {
            return LimitSeverity.Near;
        }

        return LimitSeverity.None;
    }

    private static string FormatCore(float? limitKmh, bool richText, LimitSeverity severity)
    {
        if (limitKmh is null)
        {
            return "— Limit";
        }

        var text = $"Limit {Round(limitKmh.Value)}";
        if (!richText || severity == LimitSeverity.None)
        {
            return text;
        }

        var color = severity == LimitSeverity.Over ? CriticalColor : WarningColor;
        return $"<color={color}>{text}</color>";
    }

    private static int Round(float value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}

public enum LimitSeverity
{
    None,
    Near,
    Over,
}
