using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Loco-anchored train summary bar (totals as if standing in the locomotive).
/// Chip order is center-weighted IA (4.7): peripherals outside, Speed/Limit at mid-string.
/// Optional fluid-gated <paramref name="nextStation"/> appends at the end (4.5).
/// </summary>
public static class TrainHudLine
{
    public static string Format(
        string fuel,
        string oil,
        string mass,
        string grade,
        string load,
        string speed,
        string limit,
        string motors,
        string handbrakes,
        string cars,
        string? nextStation = null)
    {
        var parts = new List<string>
        {
            fuel, oil, mass, grade, load, speed, limit, motors, handbrakes, cars,
        };
        if (!string.IsNullOrEmpty(nextStation))
        {
            parts.Add(nextStation!);
        }

        return MonitorHudLine.Join(parts);
    }

    public static string NullLine() =>
        Format(
            FluidDisplay.FormatFuel(null),
            FluidDisplay.FormatOil(null),
            TonnageDisplay.FormatFromKilograms(null),
            GradeDisplay.FormatPercent(null),
            LoadDisplay.Format(null),
            SpeedDisplay.FormatFromMetersPerSecond(null),
            SpeedLimitDisplay.Format(null),
            MotorDisplay.Format(null),
            HandbrakeDisplay.FormatTotal(null),
            CarsDisplay.Format(null));
}
