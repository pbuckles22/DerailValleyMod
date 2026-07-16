using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Read-only access to the player's car speed. No game-state writes.
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
            // Fail closed for Monitor reads — never throw into the frame loop.
            return null;
        }
    }

    public static string CurrentSpeedLabel() =>
        SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond());
}
