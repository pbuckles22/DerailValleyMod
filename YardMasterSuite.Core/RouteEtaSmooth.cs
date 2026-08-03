using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Align ETA (3.5 #3): plan-scaled remaining + soft schedule lag; clamp <b>arrival</b>
/// while moving so the chip cannot jump minutes on throttle/brake. While stopped/crawl
/// (no pace hint), displayed remaining freezes — it must not count down on wall clock.
/// </summary>
public static class RouteEtaSmooth
{
    public const float DefaultMaxArrivalStepSeconds = 12f;

    /// <summary>How quickly lag tracks pace−plan (pace is advisory only).</summary>
    public const float DefaultLagAlpha = 0.08f;

    /// <summary>Cap stored lag so a wild pace sample cannot bank a multi-minute debt.</summary>
    public const float MaxAbsLagSeconds = 180f;

    /// <summary>
    /// One ~1 Hz tick. <paramref name="previousArrivalUnscaled"/> &lt; 0 means unset.
    /// <paramref name="previousDisplayedRem"/> holds the frozen chip while stopped.
    /// Returns display remaining seconds (≥ 0).
    /// </summary>
    public static float Tick(
        float planRemainingSeconds,
        float? paceHintSeconds,
        float nowUnscaled,
        ref float scheduleLagSeconds,
        ref float previousArrivalUnscaled,
        ref float previousPlanRemainingSeconds,
        ref float previousDisplayedRem,
        float maxArrivalStepSeconds = DefaultMaxArrivalStepSeconds,
        float lagAlpha = DefaultLagAlpha)
    {
        if (float.IsNaN(planRemainingSeconds) || float.IsInfinity(planRemainingSeconds)
            || planRemainingSeconds < 0f)
        {
            planRemainingSeconds = 0f;
        }

        var moving = paceHintSeconds is float paceValid
            && !float.IsNaN(paceValid)
            && !float.IsInfinity(paceValid)
            && paceValid >= 0f;

        if (moving)
        {
            var desiredLag = paceHintSeconds!.Value - planRemainingSeconds;
            scheduleLagSeconds += lagAlpha * (desiredLag - scheduleLagSeconds);
        }
        else
        {
            // Stopped / crawl — bleed lag so it does not bank while rem meters freeze.
            scheduleLagSeconds += lagAlpha * (0f - scheduleLagSeconds);
        }

        if (scheduleLagSeconds > MaxAbsLagSeconds)
        {
            scheduleLagSeconds = MaxAbsLagSeconds;
        }
        else if (scheduleLagSeconds < -MaxAbsLagSeconds)
        {
            scheduleLagSeconds = -MaxAbsLagSeconds;
        }

        var targetRem = planRemainingSeconds + scheduleLagSeconds;
        if (targetRem < 0f)
        {
            targetRem = 0f;
        }

        // First sample — seed chip from plan (+ lag).
        if (previousArrivalUnscaled < 0f
            || float.IsNaN(previousArrivalUnscaled)
            || float.IsInfinity(previousArrivalUnscaled)
            || previousDisplayedRem < 0f)
        {
            previousArrivalUnscaled = nowUnscaled + targetRem;
            previousPlanRemainingSeconds = planRemainingSeconds;
            previousDisplayedRem = targetRem;
            return targetRem;
        }

        // Stopped: freeze displayed remaining (no wall-clock countdown).
        if (!moving)
        {
            previousArrivalUnscaled = nowUnscaled + previousDisplayedRem;
            previousPlanRemainingSeconds = planRemainingSeconds;
            return previousDisplayedRem;
        }

        if (maxArrivalStepSeconds < 0f)
        {
            maxArrivalStepSeconds = 0f;
        }

        var arrival = nowUnscaled + targetRem;
        var lo = previousArrivalUnscaled - maxArrivalStepSeconds;
        var hi = previousArrivalUnscaled + maxArrivalStepSeconds;
        // Plan rem not increasing ⇒ we are not farther from dest — forbid ETA climb.
        var planDelta = planRemainingSeconds - previousPlanRemainingSeconds;
        if (planDelta <= 0.5f)
        {
            hi = previousArrivalUnscaled;
        }

        if (arrival < lo)
        {
            arrival = lo;
        }
        else if (arrival > hi)
        {
            arrival = hi;
        }

        previousArrivalUnscaled = arrival;
        previousPlanRemainingSeconds = planRemainingSeconds;
        var rem = arrival - nowUnscaled;
        if (rem < 0f)
        {
            rem = 0f;
        }

        previousDisplayedRem = rem;
        return rem;
    }

    public static void Reset(
        ref float scheduleLagSeconds,
        ref float previousArrivalUnscaled,
        ref float previousPlanRemainingSeconds,
        ref float previousDisplayedRem)
    {
        scheduleLagSeconds = 0f;
        previousArrivalUnscaled = -1f;
        previousPlanRemainingSeconds = -1f;
        previousDisplayedRem = -1f;
    }

    /// <summary>
    /// Raw rem÷speed hint for lag only — not the displayed ETA.
    /// Null when too slow / unknown (caller keeps prior lag).
    /// </summary>
    public static float? PaceHintSeconds(float remainingMeters, float speedKmh, float minSpeedKmh = 8f)
        => PathRouteDebug.LiveEtaSeconds(remainingMeters, speedKmh, minSpeedKmh);

    /// <summary>Max |Δ remaining| over a 1 s wall tick under arrival clamp (for tests / docs).</summary>
    public static float MaxRemainingJumpPerSecond(float maxArrivalStepSeconds = DefaultMaxArrivalStepSeconds)
        => maxArrivalStepSeconds + 1f;
}
