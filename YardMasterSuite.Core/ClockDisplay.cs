using System;

namespace YardMasterSuite.Core;

/// <summary>World-session clock chip from in-game <see cref="DateTime"/> (hour:minute).</summary>
public static class ClockDisplay
{
    public static string Format(int hour24, int minute)
    {
        if (hour24 < 0 || hour24 > 23 || minute < 0 || minute > 59)
        {
            return "— Clock";
        }

        return "Clock " + hour24.ToString("00") + ":" + minute.ToString("00");
    }

    public static string Format(DateTime? timeOfDay)
    {
        if (timeOfDay is null)
        {
            return "— Clock";
        }

        var t = timeOfDay.Value;
        return Format(t.Hour, t.Minute);
    }
}
