using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure speed-limit formatting for the train HUD bar (geometry / board km/h).
/// Yellow within <see cref="NearThresholdKmh"/> of the limit; red when over.
/// Optional ▲/▼ trend for the next posted change (**1.11**) — no second km/h chip.
/// </summary>
public static class SpeedLimitDisplay
{
    /// <summary>Yellow band starts this many km/h below the posted limit (inclusive at limit).</summary>
    public const float NearThresholdKmh = 5f;

    /// <summary>ASCII glyphs — Unity default GUI font often lacks ▲/▼.</summary>
    public const string UpArrow = "^";
    public const string DownArrow = "v";

    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";
    public const string UpColor = "#55FF55";
    public const string DownColor = "#FFD400";

    public static string Format(float? limitKmh, LimitTrend trend = LimitTrend.None) =>
        FormatCore(limitKmh, richText: false, severity: LimitSeverity.None, trend);

    public static string FormatHud(float? speedKmh, float? limitKmh, LimitTrend trend = LimitTrend.None) =>
        FormatCore(limitKmh, richText: true, severity: Severity(speedKmh, limitKmh), trend);

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

    public static LimitTrend TrendFrom(float? currentKmh, float? nextKmh)
    {
        if (currentKmh is null || nextKmh is null)
        {
            return LimitTrend.None;
        }

        var current = Round(currentKmh.Value);
        var next = Round(nextKmh.Value);
        if (next > current)
        {
            return LimitTrend.Up;
        }

        if (next < current)
        {
            return LimitTrend.Down;
        }

        return LimitTrend.None;
    }

    private static string FormatCore(
        float? limitKmh,
        bool richText,
        LimitSeverity severity,
        LimitTrend trend)
    {
        if (limitKmh is null)
        {
            return "— Limit";
        }

        var text = $"Limit {Round(limitKmh.Value)}";
        if (richText && severity != LimitSeverity.None)
        {
            var color = severity == LimitSeverity.Over ? CriticalColor : WarningColor;
            text = $"<color={color}>{text}</color>";
        }

        return text + FormatTrendSuffix(trend, richText);
    }

    private static string FormatTrendSuffix(LimitTrend trend, bool richText)
    {
        if (trend == LimitTrend.None)
        {
            return string.Empty;
        }

        var arrow = trend == LimitTrend.Up ? UpArrow : DownArrow;
        if (!richText)
        {
            return $" {arrow}";
        }

        var color = trend == LimitTrend.Up ? UpColor : DownColor;
        return $" <b><color={color}>{arrow}</color></b>";
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

public enum LimitTrend
{
    None,
    Up,
    Down,
}
