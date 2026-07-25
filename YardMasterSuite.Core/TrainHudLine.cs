namespace YardMasterSuite.Core;

/// <summary>
/// Loco-anchored train summary bar (totals as if standing in the locomotive).
/// Chip order is center-weighted IA (4.7): peripherals outside, Speed/Limit at mid-string.
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
        string cars) =>
        MonitorHudLine.Join(new[]
        {
            fuel, oil, mass, grade, load, speed, limit, motors, handbrakes, cars,
        });

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
