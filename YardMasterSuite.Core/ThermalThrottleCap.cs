using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure thermal throttle soft-cap for <b>2.2</b>: when Motors are Hot, never raise throttle;
/// roll current down toward a band ceiling (Warning milder, Critical harder). Cool → passthrough.
/// </summary>
public static class ThermalThrottleCap
{
    /// <summary>Ceiling while cab MU Warning (barely yellow) — keep crawling on grade.</summary>
    public const float DefaultMaxWhenWarning = 0.75f;

    /// <summary>Ceiling while cab MU Critical — cool-down without stalling as hard as 40%.</summary>
    public const float DefaultMaxWhenCritical = 0.55f;

    /// <summary>Legacy alias for Critical ceiling (hard-cap MVP).</summary>
    public const float DefaultMaxWhenHot = DefaultMaxWhenCritical;

    /// <summary>How fast to lower throttle toward the ceiling (fraction of notch per second).</summary>
    public const float DefaultRollbackPerSecond = 0.05f;

    /// <summary>Resolve soft-cap ceiling from cab MU temp band. Null/Nominal → no cap (1).</summary>
    public static float CeilingForBand(
        MotorCabTempBand? band,
        float maxWhenWarning = DefaultMaxWhenWarning,
        float maxWhenCritical = DefaultMaxWhenCritical)
    {
        if (band is null || band == MotorCabTempBand.Nominal)
        {
            return 1f;
        }

        return band is MotorCabTempBand.Critical or MotorCabTempBand.WarningAndCritical
            ? Clamp01(maxWhenCritical)
            : Clamp01(maxWhenWarning);
    }

    /// <summary>
    /// Soft-roll toward <paramref name="ceiling"/> when Hot: never raise; step down by
    /// <paramref name="rollbackPerSecond"/> × <paramref name="deltaTime"/> until at ceiling.
    /// Cool motors → passthrough.
    /// </summary>
    public static float ComputeDesiredThrottle(
        float currentThrottle,
        bool motorsHot,
        float ceiling,
        float deltaTime,
        float rollbackPerSecond = DefaultRollbackPerSecond)
    {
        var current = Clamp01(currentThrottle);
        if (!motorsHot)
        {
            return current;
        }

        var max = Clamp01(ceiling);
        if (current <= max)
        {
            return current;
        }

        var step = Math.Max(0f, rollbackPerSecond) * Math.Max(0f, deltaTime);
        if (step <= 0f)
        {
            // No time / zero rate → hard snap to ceiling (FixedUpdate should pass dt > 0).
            return max;
        }

        return Math.Max(max, current - step);
    }

    /// <summary>Backward-compatible hard min(current, max) when Hot (no soft rollback).</summary>
    public static float ComputeDesiredThrottle(float currentThrottle, bool motorsHot, float maxWhenHot) =>
        ComputeDesiredThrottle(currentThrottle, motorsHot, maxWhenHot, deltaTime: 0f, rollbackPerSecond: 0f);

    public static bool ShouldSoftWrite(float currentThrottle, float desiredThrottle) =>
        desiredThrottle + 1e-4f < Clamp01(currentThrottle);

    /// <summary>
    /// Safety predicates for ThreeGate (thermal may run while moving — not stationary-only).
    /// </summary>
    public static bool IsSafeToCap(
        bool hasUsableLoco,
        bool controlsPresent,
        bool controlNotBlocked,
        bool motorsHot,
        bool currentAboveCap) =>
        hasUsableLoco
        && controlsPresent
        && controlNotBlocked
        && motorsHot
        && currentAboveCap;

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
