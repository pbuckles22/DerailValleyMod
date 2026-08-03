using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Coupler / derail stress for the train HUD bar — live <c>TrainStress</c> vs game
/// derail thresholds as a percent of threshold (worse of stress and build-up).
/// RAG: green &lt; 80%, yellow ≥ 80%, red ≥ 95% (same bands as Load).
/// </summary>
public static class StressDisplay
{
    public const float WarningThresholdPercent = 80f;
    public const float CriticalThresholdPercent = 95f;

    /// <summary>Green — well below derail threshold.</summary>
    public const string OkColor = "#55FF55";

    /// <summary>Yellow — matches Load / MU warning tone.</summary>
    public const string WarningColor = "#FFD400";

    /// <summary>Red — at or above critical band of derail threshold.</summary>
    public const string CriticalColor = "#FF5555";

    /// <summary>
    /// Worst usable ratio of stress or derail-build-up vs its game threshold, as percent.
    /// Null when neither pair is usable. May exceed 100 when over threshold.
    /// </summary>
    public static float? PercentOfThreshold(
        float? stress,
        float? stressThreshold,
        float? derailBuildUp,
        float? buildUpThreshold)
    {
        float? worst = null;
        Consider(ref worst, RatioPercent(stress, stressThreshold));
        Consider(ref worst, RatioPercent(derailBuildUp, buildUpThreshold));
        return worst;
    }

    public static string Format(float? stressPercent) =>
        FormatCore(stressPercent, richText: false);

    public static string FormatHud(float? stressPercent) =>
        FormatCore(stressPercent, richText: true);

    private static string FormatCore(float? stressPercent, bool richText)
    {
        if (stressPercent is null)
        {
            return "— Stress";
        }

        var whole = (int)Math.Round(stressPercent.Value, MidpointRounding.AwayFromZero);
        if (whole < 0)
        {
            whole = 0;
        }

        var text = $"Stress {whole} %";
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

        return $"<color={OkColor}>{text}</color>";
    }

    private static float? RatioPercent(float? value, float? threshold)
    {
        if (value is not float v
            || threshold is not float thr
            || thr < DerailRiskDebug.MinUsableThreshold)
        {
            return null;
        }

        return (v / thr) * 100f;
    }

    private static void Consider(ref float? worst, float? candidate)
    {
        if (candidate is not float c)
        {
            return;
        }

        if (worst is null || c > worst.Value)
        {
            worst = c;
        }
    }
}
