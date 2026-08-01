using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Session odometer for smoke runs: how far the consist has driven since the world session
/// started (or the accumulator was reset). Pure integration of speed × dt — no Unity refs.
/// </summary>
public static class SessionDistance
{
    /// <summary>Accumulate meters from absolute speed (km/h) and elapsed seconds.</summary>
    public static float Step(float metersSoFar, float speedKmh, float deltaSeconds)
    {
        if (metersSoFar < 0f)
        {
            metersSoFar = 0f;
        }

        if (deltaSeconds <= 0f || speedKmh <= 0f)
        {
            return metersSoFar;
        }

        return metersSoFar + (SpeedDisplay.ToMetersPerSecond(speedKmh) * deltaSeconds);
    }

    /// <summary>Compact HUD chip: meters under 1 km, then one-decimal kilometres.</summary>
    public static string Format(float meters)
    {
        if (meters < 0f)
        {
            meters = 0f;
        }

        if (meters < 1000f)
        {
            return $"Drive {(int)Math.Round(meters, MidpointRounding.AwayFromZero)} m";
        }

        return $"Drive {meters / 1000f:0.0} km";
    }
}
