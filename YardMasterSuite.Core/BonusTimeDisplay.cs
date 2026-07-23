using System;
using System.Globalization;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure bonus-time remaining formatting for the Active Job HUD (4.8).
/// Input is real-time seconds (job TimeLimit / time-on-job).
/// </summary>
public static class BonusTimeDisplay
{
    public const float WarningSeconds = 5f * 60f;
    public const float CriticalSeconds = 60f;
    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";

    public static float? RemainingSeconds(float? timeLimitSeconds, float? timeOnJobSeconds)
    {
        if (timeLimitSeconds is null || timeLimitSeconds.Value <= 0f)
        {
            return null;
        }

        var elapsed = timeOnJobSeconds ?? 0f;
        return timeLimitSeconds.Value - elapsed;
    }

    public static string Format(float? remainingSeconds, bool richText = false)
    {
        if (remainingSeconds is null)
        {
            return "— Bonus";
        }

        var secs = remainingSeconds.Value;
        var text = secs <= 0f ? "Bonus 0:00" : $"Bonus {FormatClock(secs)}";
        if (!richText)
        {
            return text;
        }

        if (secs <= 0f || secs < CriticalSeconds)
        {
            return $"<color={CriticalColor}>{text}</color>";
        }

        if (secs < WarningSeconds)
        {
            return $"<color={WarningColor}>{text}</color>";
        }

        return text;
    }

    internal static string FormatClock(float seconds)
    {
        var total = (int)Math.Floor(Math.Max(0.0, seconds));
        var h = total / 3600;
        var m = (total % 3600) / 60;
        var s = total % 60;
        if (h > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", h, m, s);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", m, s);
    }
}
