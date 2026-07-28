using DV.Simulation.Controllers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// <b>2.2</b> Thermal governor — soft-roll lead loco throttle when Motors are Hot.
/// Warning → ~65% ceiling; Critical → ~40%. Fail closed via <see cref="ThreeGate"/>.
/// </summary>
internal static class ThermalGovernor
{
    private static bool _wasCapping;
    private static ThreeGateAbortReason _lastAbort = ThreeGateAbortReason.None;
    private static string? _lastCapLabel;

    /// <summary>
    /// Call from FixedUpdate while world session active. Returns a discrete T2 line when state changes.
    /// </summary>
    internal static string? Tick()
    {
        try
        {
            var motors = TelemetryReader.TryGetMotorStatus();
            var motorsHot = motors == MotorStatus.Hot;
            var band = TelemetryReader.TryGetCabTempBandForGovernor();
            var ceiling = motorsHot
                ? (band is null || band == MotorCabTempBand.Nominal
                    ? ThermalThrottleCap.DefaultMaxWhenCritical
                    : ThermalThrottleCap.CeilingForBand(band))
                : 1f;

            var loco = TelemetryReader.TryGetUsableLocoForGovernor();
            var throttle = loco?.SimController?.controlsOverrider?.Throttle;
            var controlsPresent = throttle != null;
            var blocked = throttle != null && throttle.IsControlBlocked;
            var current = throttle != null ? throttle.Value : 0f;
            var desired = ThermalThrottleCap.ComputeDesiredThrottle(
                current,
                motorsHot,
                ceiling,
                deltaTime: Time.fixedDeltaTime);
            var aboveCap = ThermalThrottleCap.ShouldSoftWrite(current, desired);

            var safety = ThermalThrottleCap.IsSafeToCap(
                hasUsableLoco: loco != null,
                controlsPresent: controlsPresent,
                controlNotBlocked: !blocked,
                motorsHot: motorsHot,
                currentAboveCap: aboveCap);

            if (!safety)
            {
                return EndCappingIfNeeded(ThreeGateAbortReason.Safety);
            }

            var result = ThreeGate.TryApply(
                integrityOk: loco != null && HudWorldSession.IsActive(PlayerManager.PlayerTransform != null),
                stateRegistryOk: controlsPresent,
                safetyOk: true,
                softWrite: () =>
                {
                    throttle!.Set(desired);
                    return true;
                });

            if (result.Applied)
            {
                return BeginCappingIfNeeded(ceiling, band);
            }

            return EndCappingIfNeeded(result.AbortReason);
        }
        catch
        {
            return EndCappingIfNeeded(ThreeGateAbortReason.SoftWrite);
        }
    }

    private static string? BeginCappingIfNeeded(float ceiling, MotorCabTempBand? band)
    {
        var label = band switch
        {
            MotorCabTempBand.Warning => "Warning",
            MotorCabTempBand.Critical => "Critical",
            MotorCabTempBand.WarningAndCritical => "Critical",
            _ => "Hot",
        };
        var line = $"T2 thermal: soft-cap → {ceiling:0.##} ({label})";

        if (_wasCapping && _lastCapLabel == line)
        {
            return null;
        }

        _wasCapping = true;
        _lastAbort = ThreeGateAbortReason.None;
        _lastCapLabel = line;
        return line;
    }

    private static string? EndCappingIfNeeded(ThreeGateAbortReason abort)
    {
        if (!_wasCapping && abort == _lastAbort)
        {
            return null;
        }

        if (_wasCapping)
        {
            _wasCapping = false;
            _lastCapLabel = null;
            _lastAbort = abort;
            return abort == ThreeGateAbortReason.None || abort == ThreeGateAbortReason.Safety
                ? "T2 thermal: cap release"
                : $"T2 thermal: abort {abort}";
        }

        _lastAbort = abort;
        return null;
    }
}
