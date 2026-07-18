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
        string handbrakes) =>
        MonitorHudLine.Join(new[] { speed, grade, mass, cars, handbrakes });

    public static string NullLine() =>
        Format(
            SpeedDisplay.FormatFromMetersPerSecond(null),
            GradeDisplay.FormatPercent(null),
            TonnageDisplay.FormatFromKilograms(null),
            CarsDisplay.Format(null),
            HandbrakeDisplay.FormatTotal(null));
}
