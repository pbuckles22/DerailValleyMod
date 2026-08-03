using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure speed-limit formatting for the train HUD bar (**1.17**).
/// Yellow from 10 km/h below through 5 km/h above the limit; red beyond that upper band.
/// Optional Next with distance — Limit number only (no Posted/Recommended labels).
/// </summary>
public static class SpeedLimitDisplay
{
    /// <summary>Yellow band begins this many km/h below the Limit (inclusive).</summary>
    public const float NearBelowKmh = 10f;

    /// <summary>Yellow band extends this many km/h above the Limit (inclusive).</summary>
    public const float NearAboveKmh = 5f;

    /// <summary>Distances at or above this use km in the Next chip.</summary>
    public const float NextKmThresholdMeters = 1000f;

    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";

    /// <summary>
    /// Optional Next with distance — meters only when close enough (<see cref="NextLimitReveal"/>).
    /// </summary>
    public static string Format(
        float? limitKmh,
        float? nextKmh = null,
        float? nextDistanceMeters = null,
        float massTonnes = 40f) =>
        FormatCore(
            limitKmh,
            richText: false,
            severity: LimitSeverity.None,
            nextKmh,
            nextDistanceMeters,
            massTonnes);

    public static string FormatHud(
        float? speedKmh,
        float? limitKmh,
        float? nextKmh = null,
        float? nextDistanceMeters = null,
        float massTonnes = 40f) =>
        FormatCore(
            limitKmh,
            richText: true,
            severity: Severity(speedKmh, limitKmh),
            nextKmh,
            nextDistanceMeters,
            massTonnes);

    public static LimitSeverity Severity(float? speedKmh, float? limitKmh)
    {
        if (speedKmh is null || limitKmh is null)
        {
            return LimitSeverity.None;
        }

        var speed = Round(speedKmh.Value);
        var limit = Round(limitKmh.Value);
        if (speed > limit + NearAboveKmh)
        {
            return LimitSeverity.Over;
        }

        if (speed >= limit - NearBelowKmh)
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

    public static string FormatNextDistance(float meters)
    {
        if (meters >= NextKmThresholdMeters)
        {
            return $"{meters / 1000f:0.0}km";
        }

        return $"{Round(meters)}m";
    }

    private static string FormatCore(
        float? limitKmh,
        bool richText,
        LimitSeverity severity,
        float? nextKmh,
        float? nextDistanceMeters,
        float massTonnes)
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

        if (nextKmh is float next && nextDistanceMeters is float along && along > 0f)
        {
            if (NextLimitReveal.ShowDistance(along, limitKmh.Value, next, massTonnes))
            {
                text += $" | Next {Round(next)} ({FormatNextDistance(along)})";
            }
            else
            {
                text += $" | Next {Round(next)}";
            }
        }

        return text;
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
