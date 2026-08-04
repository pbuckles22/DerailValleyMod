namespace YardMasterSuite.Core;

/// <summary>
/// Loco-anchored train summary bar (totals as if standing in the locomotive).
/// Chip order is center-weighted IA (4.7): peripherals outside, Speed/Limit at mid-string.
/// Cab levers sit just before Speed (Load · Rev · Throttle · Indy · TrainBrake).
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
        string? backup = null,
        string? freeMotion = null,
        string? drive = null,
        string? reverser = null,
        string? throttle = null,
        string? indy = null,
        string? trainBrake = null,
        string? stress = null) =>
        MonitorHudLine.Join(new[]
        {
            fuel, oil, mass, grade, load,
            reverser ?? string.Empty,
            throttle ?? string.Empty,
            indy ?? string.Empty,
            trainBrake ?? string.Empty,
            speed, limit,
            motors,
            stress ?? string.Empty,
            freeMotion ?? string.Empty,
            handbrakes, cars,
            backup ?? string.Empty,
            drive ?? string.Empty,
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
            CarsDisplay.Format(null),
            reverser: ReverserDisplay.Format(null),
            throttle: CabLeverDisplay.FormatThrottle(null),
            indy: CabLeverDisplay.FormatIndy(null),
            trainBrake: CabLeverDisplay.FormatTrainBrake(null),
            stress: StressDisplay.Format(null));
}
