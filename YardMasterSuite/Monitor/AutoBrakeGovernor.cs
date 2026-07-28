using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// <b>2.3</b> Auto-brake — on engine on→off, soft-roll train + independent to full and
/// throttle to idle (any speed). Never auto-releases on start. Handbrakes untouched.
/// Fail closed via <see cref="ThreeGate"/>.
/// </summary>
internal static class AutoBrakeGovernor
{
    private static bool _wasEngineOn;
    private static AutoBrakePhase _phase = AutoBrakePhase.Idle;
    private static ThreeGateAbortReason _lastAbort = ThreeGateAbortReason.None;

    /// <summary>True while soft-securing controls this session.</summary>
    internal static bool IsApplying => _phase == AutoBrakePhase.Applying;

    /// <summary>
    /// Call from FixedUpdate while world session active. Returns a discrete T2 line when state changes.
    /// </summary>
    internal static string? Tick()
    {
        var previous = _phase;
        try
        {
            var loco = TelemetryReader.TryGetUsableLocoForGovernor();
            var controls = loco?.SimController?.controlsOverrider;
            var brake = controls?.Brake;
            var ind = controls?.IndependentBrake;
            var throttle = controls?.Throttle;
            var engineOn = controls?.EngineOnReader != null && controls.EngineOnReader.IsOn;
            var engineOff = !engineOn;
            var falling = AutoBrakePark.DetectEngineOffFallingEdge(_wasEngineOn, engineOn);
            _wasEngineOn = engineOn;

            var trainVal = brake != null ? brake.Value : AutoBrakePark.FullApply;
            var indVal = ind != null ? ind.Value : AutoBrakePark.FullApply;
            var throttleVal = throttle != null ? throttle.Value : 0f;
            var needsWork = AutoBrakePark.SessionNeedsWork(
                brake != null ? trainVal : AutoBrakePark.FullApply,
                ind != null ? indVal : AutoBrakePark.FullApply,
                throttle != null ? throttleVal : 0f);
            var controlsPresent = brake != null || ind != null || throttle != null;
            var blocked =
                (brake != null && brake.IsControlBlocked)
                || (ind != null && ind.IsControlBlocked)
                || (throttle != null && throttle.IsControlBlocked);

            var safeToStart = AutoBrakePark.IsSafeToApply(
                hasUsableLoco: loco != null,
                controlsPresent: controlsPresent,
                controlNotBlocked: !blocked,
                engineOff: engineOff,
                sessionNeedsWork: needsWork);

            var continueOk =
                loco != null
                && controlsPresent
                && !blocked
                && engineOff;

            var phaseSafe = _phase == AutoBrakePhase.Applying ? continueOk : safeToStart;
            _phase = AutoBrakePark.NextPhase(_phase, falling, engineOff, phaseSafe, needsWork);

            if (_phase != AutoBrakePhase.Applying)
            {
                return EndIfNeeded(previous, needsWork, ThreeGateAbortReason.Safety);
            }

            var dt = Time.fixedDeltaTime;
            var desiredTrain = AutoBrakePark.ComputeDesiredBrake(trainVal, applying: true, dt);
            var desiredInd = AutoBrakePark.ComputeDesiredBrake(indVal, applying: true, dt);
            var desiredThrottle = AutoBrakePark.ComputeDesiredThrottle(throttleVal, applying: true, dt);
            var writeTrain = brake != null && AutoBrakePark.ShouldRaise(trainVal, desiredTrain);
            var writeInd = ind != null && AutoBrakePark.ShouldRaise(indVal, desiredInd);
            var writeThrottle = throttle != null && AutoBrakePark.ShouldLower(throttleVal, desiredThrottle);

            if (!writeTrain && !writeInd && !writeThrottle)
            {
                return BeginIfNeeded(previous);
            }

            var result = ThreeGate.TryApply(
                integrityOk: loco != null && HudWorldSession.IsActive(PlayerManager.PlayerTransform != null),
                stateRegistryOk: controlsPresent,
                safetyOk: true,
                softWrite: () =>
                {
                    if (writeTrain)
                    {
                        brake!.Set(desiredTrain);
                    }

                    if (writeInd)
                    {
                        ind!.Set(desiredInd);
                    }

                    if (writeThrottle)
                    {
                        throttle!.Set(desiredThrottle);
                    }

                    return true;
                });

            if (result.Applied)
            {
                return BeginIfNeeded(previous);
            }

            _phase = AutoBrakePhase.Idle;
            return EndIfNeeded(previous, needsWork, result.AbortReason);
        }
        catch
        {
            _phase = AutoBrakePhase.Idle;
            return EndIfNeeded(previous, needsApply: true, ThreeGateAbortReason.SoftWrite);
        }
    }

    private static string? BeginIfNeeded(AutoBrakePhase previous)
    {
        if (previous == AutoBrakePhase.Applying || _phase != AutoBrakePhase.Applying)
        {
            return null;
        }

        _lastAbort = ThreeGateAbortReason.None;
        return "T2 autobrake: applying";
    }

    private static string? EndIfNeeded(
        AutoBrakePhase previous,
        bool needsApply,
        ThreeGateAbortReason abort)
    {
        if (previous != AutoBrakePhase.Applying)
        {
            return null;
        }

        if (!needsApply)
        {
            _lastAbort = ThreeGateAbortReason.None;
            return "T2 autobrake: apply done";
        }

        if (abort == ThreeGateAbortReason.None || abort == ThreeGateAbortReason.Safety)
        {
            if (_lastAbort == ThreeGateAbortReason.Safety)
            {
                return null;
            }

            _lastAbort = ThreeGateAbortReason.Safety;
            return "T2 autobrake: abort Safety";
        }

        if (abort == _lastAbort)
        {
            return null;
        }

        _lastAbort = abort;
        return $"T2 autobrake: abort {abort}";
    }
}
