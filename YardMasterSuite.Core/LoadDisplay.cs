using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure traction-load formatting for the train HUD bar (current amp / max amp).
/// </summary>
public static class LoadDisplay
{
    public const float WarningThresholdPercent = 80f;
    public const float CriticalThresholdPercent = 95f;

    /// <summary>Yellow — matches MU warning tone.</summary>
    public const string WarningColor = "#FFD400";

    /// <summary>Red — load at or above critical threshold.</summary>
    public const string CriticalColor = "#FF5555";

    public static float? PercentFromAmps(float? currentAmps, float? maxAmps)
    {
        if (currentAmps is null || maxAmps is null || maxAmps.Value <= 0f)
        {
            return null;
        }

        return ClampPercent(currentAmps.Value / maxAmps.Value * 100f);
    }

    public static float? PercentFromNormalized(float? normalized01)
    {
        if (normalized01 is null)
        {
            return null;
        }

        return ClampPercent(normalized01.Value * 100f);
    }

    public static string Format(float? loadPercent) =>
        FormatCore(loadPercent, richText: false);

    public static string FormatHud(float? loadPercent) =>
        FormatCore(loadPercent, richText: true);

    private static string FormatCore(float? loadPercent, bool richText)
    {
        if (loadPercent is null)
        {
            return "— Load";
        }

        var whole = (int)Math.Round(loadPercent.Value, MidpointRounding.AwayFromZero);
        var text = $"Load {whole} %";
        if (!richText)
        {
            return text;
        }

        if (whole >= CriticalThresholdPercent)
        {
            return $"<color={CriticalColor}>{text}</color>";
        }

        if (whole >= WarningThresholdPercent)
        {
            return $"<color={WarningColor}>{text}</color>";
        }

        return text;
    }

    private static float ClampPercent(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 100f)
        {
            return 100f;
        }

        return value;
    }
}
