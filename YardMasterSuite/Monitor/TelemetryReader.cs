using System;
using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Read-only telemetry from the player's car / trainset. No game-state writes.
/// </summary>
public static class TelemetryReader
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
    /// Number of cars in the consist with handbrake applied (position above threshold).
    /// </summary>
    public static int? TryGetHandbrakeAppliedCount()
    {
        try
        {
            var car = PlayerManager.Car;
            if (car == null)
            {
                return null;
            }

            var positions = new List<float>();
            var set = car.trainset;
            if (set?.cars != null && set.cars.Count > 0)
            {
                foreach (var c in set.cars)
                {
                    var brakes = c?.brakeSystem;
                    if (brakes != null && brakes.hasHandbrake)
                    {
                        positions.Add(brakes.handbrakePosition);
                    }
                }
            }
            else if (car.brakeSystem != null && car.brakeSystem.hasHandbrake)
            {
                positions.Add(car.brakeSystem.handbrakePosition);
            }

            return HandbrakeDisplay.CountApplied(positions);
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
}
