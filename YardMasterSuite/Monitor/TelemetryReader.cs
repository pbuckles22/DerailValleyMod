using System;
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

    public static string CurrentHudLine() =>
        MonitorHudLine.Join(new[]
        {
            SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
        });
}
