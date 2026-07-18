namespace YardMasterSuite.Core;

/// <summary>
/// Single-car HUD bar (standing on a car, or look-at when on foot).
/// </summary>
public static class LocalCarHudLine
{
    public static string Format(
        string pipe,
        string handbrake,
        string couplers,
        string carNumber,
        string job) =>
        MonitorHudLine.Join(new[] { pipe, handbrake, couplers, carNumber, job });
}