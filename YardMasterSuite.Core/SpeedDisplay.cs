using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure speed formatting for Monitor HUD (no Unity / game refs).
/// Game speeds are meters/second; DV UI uses km/h.
/// </summary>
public static class SpeedDisplay
{
    public const float MetersPerSecondToKmh = 3.6f;

    public static float ToKilometersPerHour(float metersPerSecond) =>
        metersPerSecond * MetersPerSecondToKmh;

    public static string FormatKmh(float kilometersPerHour) =>
        $"{RoundHalfAwayFromZero(kilometersPerHour)} km/h";

    public static string FormatFromMetersPerSecond(float? metersPerSecond) =>
        metersPerSecond is null
            ? "— km/h"
            : FormatKmh(ToKilometersPerHour(metersPerSecond.Value));

    private static int RoundHalfAwayFromZero(float value) =>
        (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
