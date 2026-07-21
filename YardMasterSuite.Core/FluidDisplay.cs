using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure fuel/oil formatting for the train HUD bar (container amount / capacity).
/// Yellow / red when either fluid is below its threshold (paired cue).
/// </summary>
public static class FluidDisplay
{
    public const float WarningThresholdPercent = 20f;
    public const float CriticalThresholdPercent = 5f;

    /// <summary>Yellow — matches MU warning tone / Load warning.</summary>
    public const string WarningColor = "#FFD400";

    /// <summary>Red — matches Load critical tone.</summary>
    public const string CriticalColor = "#FF5555";

    public static float? PercentFromAmount(float? amount, float? capacity)
    {
        if (amount is null || capacity is null || capacity.Value <= 0f)
        {
            return null;
        }

        return ClampPercent(amount.Value / capacity.Value * 100f);
    }

    public static float? PercentFromNormalized(float? normalized01)
    {
        if (normalized01 is null)
        {
            return null;
        }

        return ClampPercent(normalized01.Value * 100f);
    }

    public static bool IsCritical(float? percent)
    {
        if (percent is null)
        {
            return false;
        }

        var whole = (int)Math.Round(percent.Value, MidpointRounding.AwayFromZero);
        return whole < CriticalThresholdPercent;
    }

    public static bool IsLow(float? percent)
    {
        if (percent is null)
        {
            return false;
        }

        var whole = (int)Math.Round(percent.Value, MidpointRounding.AwayFromZero);
        return whole < WarningThresholdPercent;
    }

    public static string FormatFuel(float? fuelPercent) =>
        FormatCore("Fuel", fuelPercent, richText: false, severity: FluidSeverity.None);

    public static string FormatOil(float? oilPercent) =>
        FormatCore("Oil", oilPercent, richText: false, severity: FluidSeverity.None);

    public static string FormatFuelHud(float? fuelPercent, float? oilPercent) =>
        FormatCore("Fuel", fuelPercent, richText: true, severity: PairSeverity(fuelPercent, oilPercent));

    public static string FormatOilHud(float? fuelPercent, float? oilPercent) =>
        FormatCore("Oil", oilPercent, richText: true, severity: PairSeverity(fuelPercent, oilPercent));

    private static FluidSeverity PairSeverity(float? fuelPercent, float? oilPercent)
    {
        if (IsCritical(fuelPercent) || IsCritical(oilPercent))
        {
            return FluidSeverity.Critical;
        }

        if (IsLow(fuelPercent) || IsLow(oilPercent))
        {
            return FluidSeverity.Warning;
        }

        return FluidSeverity.None;
    }

    private static string FormatCore(string label, float? percent, bool richText, FluidSeverity severity)
    {
        if (percent is null)
        {
            return $"— {label}";
        }

        var whole = (int)Math.Round(percent.Value, MidpointRounding.AwayFromZero);
        var text = $"{label} {whole} %";
        if (!richText || severity == FluidSeverity.None)
        {
            return text;
        }

        var color = severity == FluidSeverity.Critical ? CriticalColor : WarningColor;
        return $"<color={color}>{text}</color>";
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

    private enum FluidSeverity
    {
        None,
        Warning,
        Critical,
    }
}
