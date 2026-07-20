using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure fuel/oil formatting for the train HUD bar (container amount / capacity).
/// Yellow when either fluid is below the warning threshold (paired cue).
/// </summary>
public static class FluidDisplay
{
    public const float WarningThresholdPercent = 20f;

    /// <summary>Yellow — matches MU warning tone / Load warning.</summary>
    public const string WarningColor = "#FFD400";

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
        FormatCore("Fuel", fuelPercent, richText: false, warn: false);

    public static string FormatOil(float? oilPercent) =>
        FormatCore("Oil", oilPercent, richText: false, warn: false);

    public static string FormatFuelHud(float? fuelPercent, float? oilPercent) =>
        FormatCore("Fuel", fuelPercent, richText: true, warn: IsLow(fuelPercent) || IsLow(oilPercent));

    public static string FormatOilHud(float? fuelPercent, float? oilPercent) =>
        FormatCore("Oil", oilPercent, richText: true, warn: IsLow(fuelPercent) || IsLow(oilPercent));

    private static string FormatCore(string label, float? percent, bool richText, bool warn)
    {
        if (percent is null)
        {
            return $"— {label}";
        }

        var whole = (int)Math.Round(percent.Value, MidpointRounding.AwayFromZero);
        var text = $"{label} {whole} %";
        if (!richText || !warn)
        {
            return text;
        }

        return $"<color={WarningColor}>{text}</color>";
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
