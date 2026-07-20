namespace YardMasterSuite.Core;

/// <summary>
/// Loco-anchored train summary bar (totals as if standing in the locomotive).
/// </summary>
public static class TrainHudLine
{
    public static string Format(
        string speed,
        string grade,
        string mass,
        string cars,
        string handbrakes,
        string load,
        string motors,
        string fuel,
        string oil) =>
        MonitorHudLine.Join(new[] { speed, grade, mass, cars, handbrakes, load, motors, fuel, oil });

    public static string NullLine() =>
        Format(
            SpeedDisplay.FormatFromMetersPerSecond(null),
            GradeDisplay.FormatPercent(null),
            TonnageDisplay.FormatFromKilograms(null),
            CarsDisplay.Format(null),
            HandbrakeDisplay.FormatTotal(null),
            LoadDisplay.Format(null),
            MotorDisplay.Format(null),
            FluidDisplay.FormatFuel(null),
            FluidDisplay.FormatOil(null));
}
