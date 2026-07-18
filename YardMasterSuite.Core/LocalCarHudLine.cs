namespace YardMasterSuite.Core;

/// <summary>
/// Single-car HUD bar (standing on / looking at that car).
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