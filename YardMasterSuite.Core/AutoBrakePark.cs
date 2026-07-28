using System;

namespace YardMasterSuite.Core;

/// <summary>Session phase for <see cref="AutoBrakePark"/> (engine-off secure).</summary>
public enum AutoBrakePhase
{
    Idle = 0,
    Applying = 1,
}

/// <summary>
/// Pure auto-brake policy for <b>2.3</b>: on engine on→off, soft-roll train + independent
/// toward full and throttle toward idle (stopped or moving). Never auto-releases on start.
/// Handbrakes untouched. Fail closed when predicates fail.
/// </summary>
public static class AutoBrakePark
{
    /// <summary>How fast to raise brakes / lower throttle (fraction per second).</summary>
    public const float DefaultApplyPerSecond = 0.20f;

    /// <summary>Target brake lever when applying.</summary>
    public const float FullApply = 1f;

    private const float AppliedEpsilon = 1e-4f;

    public static bool DetectEngineOffFallingEdge(bool wasEngineOn, bool isEngineOn) =>
        wasEngineOn && !isEngineOn;

    /// <summary>True when either air-brake lever is below full apply.</summary>
    public static bool BrakesNeedApply(float trainBrake, float independentBrake) =>
        LeverNeedsApply(trainBrake) || LeverNeedsApply(independentBrake);

    public static bool LeverNeedsApply(float brake) =>
        Clamp01(brake) + AppliedEpsilon < FullApply;

    public static bool ThrottleNeedsIdle(float throttle) =>
        Clamp01(throttle) > AppliedEpsilon;

    /// <summary>Session incomplete until brakes full and throttle idle.</summary>
    public static bool SessionNeedsWork(float trainBrake, float independentBrake, float throttle) =>
        BrakesNeedApply(trainBrake, independentBrake) || ThrottleNeedsIdle(throttle);

    /// <summary>
    /// Soft-roll toward <see cref="FullApply"/> while <paramref name="applying"/>; otherwise passthrough.
    /// Zero delta → hard snap to full.
    /// </summary>
    public static float ComputeDesiredBrake(
        float currentBrake,
        bool applying,
        float deltaTime,
        float applyPerSecond = DefaultApplyPerSecond)
    {
        var current = Clamp01(currentBrake);
        if (!applying)
        {
            return current;
        }

        var step = Math.Max(0f, applyPerSecond) * Math.Max(0f, deltaTime);
        if (step <= 0f)
        {
            return FullApply;
        }

        return Math.Min(FullApply, current + step);
    }

    /// <summary>
    /// Soft-roll throttle toward 0 while <paramref name="applying"/>; otherwise passthrough.
    /// Zero delta → hard snap to idle.
    /// </summary>
    public static float ComputeDesiredThrottle(
        float currentThrottle,
        bool applying,
        float deltaTime,
        float applyPerSecond = DefaultApplyPerSecond)
    {
        var current = Clamp01(currentThrottle);
        if (!applying)
        {
            return current;
        }

        var step = Math.Max(0f, applyPerSecond) * Math.Max(0f, deltaTime);
        if (step <= 0f)
        {
            return 0f;
        }

        return Math.Max(0f, current - step);
    }

    public static bool ShouldRaise(float current, float desired) =>
        desired > Clamp01(current) + AppliedEpsilon;

    public static bool ShouldLower(float current, float desired) =>
        desired + AppliedEpsilon < Clamp01(current);

    public static bool IsSafeToApply(
        bool hasUsableLoco,
        bool controlsPresent,
        bool controlNotBlocked,
        bool engineOff,
        bool sessionNeedsWork) =>
        hasUsableLoco
        && controlsPresent
        && controlNotBlocked
        && engineOff
        && sessionNeedsWork;

    /// <summary>
    /// Idle → Applying on engine-off falling edge when safe and work remains.
    /// Applying → Idle when done, engine back on, or unsafe.
    /// </summary>
    public static AutoBrakePhase NextPhase(
        AutoBrakePhase current,
        bool engineOffFallingEdge,
        bool engineOff,
        bool safe,
        bool sessionStillNeedsWork)
    {
        if (current == AutoBrakePhase.Idle)
        {
            return engineOffFallingEdge && safe && sessionStillNeedsWork
                ? AutoBrakePhase.Applying
                : AutoBrakePhase.Idle;
        }

        if (!engineOff || !safe || !sessionStillNeedsWork)
        {
            return AutoBrakePhase.Idle;
        }

        return AutoBrakePhase.Applying;
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
