using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Spike: while standing on any car of the active trainset, resolve the front loco
/// (lowest trainset index) as the control target for fine-grain throttle / indy / train brake.
/// Fail closed when off-consist (no standing car) — not off-train remote.
/// </summary>
public static class OnConsistControl
{
    /// <summary>Legacy hold-nudge rate (too slow vs cab notches — prefer <see cref="StepLever"/>).</summary>
    public const float DefaultNudgePerSecond = 0.12f;

    /// <summary>Non-notched lever step — matches DV remote <c>CalcControlStepSize</c> fallback.</summary>
    public const float DefaultUnnotchedStep = 0.1f;

    /// <summary>Bottom chip while armed — uses the player's Settings → Controls cab bindings.</summary>
    public const string HudLegend =
        "On-consist: cab Throttle / Indy / TrainBrake / Reverser → front loco | Numpad . TM fuse";

    /// <summary>
    /// True when standing on the trainset but not already in the front loco cab
    /// (native cab input handles that case — avoid double-apply).
    /// </summary>
    public static bool ShouldRedirectToFrontLoco(bool playerOnCar, bool standingIsFrontLoco) =>
        playerOnCar && !standingIsFrontLoco;

    /// <summary>
    /// One reverser notch toward F (+1) or R (−1). DV uses 0 / 0.5 / 1 (R / N / F).
    /// </summary>
    public static float StepReverser(float current, int direction)
    {
        if (direction == 0)
        {
            return Clamp01(current);
        }

        var sign = direction < 0 ? -1f : 1f;
        return Clamp01(Clamp01(current) + (sign * 0.5f));
    }

    /// <summary>
    /// One cab keyboard lever step (button-down). Matches DV
    /// <c>NotchedPortIncrementalInput</c>: delta = 1/(notchCount-1).
    /// Unnotched → <see cref="DefaultUnnotchedStep"/> (~10%).
    /// </summary>
    public static float StepLever(
        float current,
        int direction,
        bool isNotched,
        float notchCount,
        float unnotchedStep = DefaultUnnotchedStep)
    {
        if (direction == 0)
        {
            return Clamp01(current);
        }

        var sign = direction < 0 ? -1f : 1f;
        float delta;
        // Cab keyboard uses (notchCount - 1), not NotchCount — that was the ~% mismatch.
        if (isNotched && notchCount > 1f && !float.IsNaN(notchCount))
        {
            delta = sign / (notchCount - 1f);
        }
        else
        {
            var step = unnotchedStep > 0f && !float.IsNaN(unnotchedStep)
                ? unnotchedStep
                : DefaultUnnotchedStep;
            delta = sign * step;
        }

        return Clamp01(Clamp01(current) + delta);
    }

    /// <summary>Toggle fuse/control 0↔1 (TM circuit knife).</summary>
    public static float Toggle01(float current) =>
        Clamp01(current) >= 0.5f ? 0f : 1f;

    /// <summary>
    /// Front loco = minimum trainset index among locos on the standing car's trainset.
    /// Null when player is not on a car or the trainset has no loco.
    /// </summary>
    public static int? ResolveFrontLocoIndex(bool playerOnCar, IReadOnlyList<int>? locoIndices)
    {
        if (!playerOnCar || locoIndices == null || locoIndices.Count == 0)
        {
            return null;
        }

        var best = locoIndices[0];
        for (var i = 1; i < locoIndices.Count; i++)
        {
            var idx = locoIndices[i];
            if (idx < best)
            {
                best = idx;
            }
        }

        return best;
    }

    /// <summary>
    /// Soft-nudge a 0–1 lever: <paramref name="direction"/> −1 / 0 / +1 × rate × dt, clamped.
    /// </summary>
    public static float Nudge(
        float current,
        int direction,
        float deltaTime,
        float ratePerSecond = DefaultNudgePerSecond)
    {
        var value = Clamp01(current);
        if (direction == 0)
        {
            return value;
        }

        var sign = direction < 0 ? -1f : 1f;
        var step = Math.Max(0f, ratePerSecond) * Math.Max(0f, deltaTime) * sign;
        return Clamp01(value + step);
    }

    /// <summary>ThreeGate safety: world + on-consist + front loco + some controls exist.</summary>
    public static bool IsSafeToWrite(
        bool worldActive,
        bool playerOnCar,
        bool hasFrontLoco,
        bool controlsPresent,
        bool controlNotBlocked) =>
        worldActive
        && playerOnCar
        && hasFrontLoco
        && controlsPresent
        && controlNotBlocked;

    /// <summary>
    /// Per-lever presence gate. On-consist spike ignores cab <paramref name="controlBlocked"/> —
    /// reach blockers are true when standing on freight, and must not mute soft-writes.
    /// </summary>
    public static bool CanWriteLever(bool controlPresent, bool controlBlocked)
    {
        _ = controlBlocked;
        return controlPresent;
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
