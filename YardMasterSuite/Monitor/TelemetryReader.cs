using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Read-only telemetry from the player's car / trainset. No game-state writes.
/// Internal — DV CommandTerminal scans public types across mod assemblies.
/// </summary>
internal static class TelemetryReader
{
    /// <summary>
    /// Absolute speed in meters/second, or null when not on a car / unavailable.
    /// </summary>
    public static float? TryGetAbsSpeedMetersPerSecond()
    {
        try
        {
            var car = PlayerManager.Car;
            if (car == null)
            {
                return null;
            }

            return car.GetAbsSpeed();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Track grade under the loco as percent (rise/run × 100). Positive = climbing along car forward.
    /// </summary>
    public static float? TryGetGradePercent()
    {
        try
        {
            var car = PlayerManager.Car;
            if (car == null)
            {
                return null;
            }

            var f = car.transform.forward;
            return GradeDisplay.PercentFromDirection(f.x, f.y, f.z);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Total trainset mass in kilograms, or the player's car alone if no trainset.
    /// </summary>
    public static float? TryGetConsistMassKilograms()
    {
        try
        {
            var car = PlayerManager.Car;
            if (car == null)
            {
                return null;
            }

            var set = car.trainset;
            if (set?.cars != null && set.cars.Count > 0)
            {
                float total = 0f;
                foreach (var c in set.cars)
                {
                    if (c?.massController != null)
                    {
                        total += c.massController.TotalMass;
                    }
                }

                return total;
            }

            return car.massController != null ? car.massController.TotalMass : (float?)null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Brake pipe pressure (bar) on the player's car.
    /// </summary>
    public static float? TryGetBrakePipePressureBar()
    {
        try
        {
            var brakes = PlayerManager.Car?.brakeSystem;
            if (brakes == null)
            {
                return null;
            }

            return brakes.brakePipePressure;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handbrake state for the player's car only (1 applied, 0 released).
    /// Consist-wide handbrake count is CMD-01b scope.
    /// </summary>
    public static int? TryGetHandbrakeAppliedCount()
    {
        try
        {
            var brakes = PlayerManager.Car?.brakeSystem;
            if (brakes == null || !brakes.hasHandbrake)
            {
                return null;
            }

            return HandbrakeDisplay.IsApplied(brakes.handbrakePosition) ? 1 : 0;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Whether the player's front coupler is coupled.
    /// </summary>
    public static bool? TryGetFrontCoupled()
    {
        try
        {
            var coupler = PlayerManager.Car?.frontCoupler;
            return coupler == null ? (bool?)null : coupler.IsCoupled();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Whether the player's rear coupler is coupled.
    /// </summary>
    public static bool? TryGetRearCoupled()
    {
        try
        {
            var coupler = PlayerManager.Car?.rearCoupler;
            return coupler == null ? (bool?)null : coupler.IsCoupled();
        }
        catch
        {
            return null;
        }
    }

    public static string CurrentHudLine() =>
        MonitorHudLine.Join(new[]
        {
            SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.Format(TryGetFrontCoupled(), TryGetRearCoupled()),
        });

    /// <summary>
    /// Integrity fields only — for Tier 2 Player.log snapshots (not speed/grade/tonnage).
    /// Internal so DV CommandTerminal does not reflect over Core return types.
    /// </summary>
    internal static IntegrityDebugSnapshot CurrentIntegrityDebugSnapshot()
    {
        var onCar = false;
        try
        {
            onCar = PlayerManager.Car != null;
        }
        catch
        {
            onCar = false;
        }

        return new IntegrityDebugSnapshot(
            onCar,
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.Format(TryGetFrontCoupled(), TryGetRearCoupled()));
    }
}
