using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure helpers for turntable tap-to-lock (arc distance along the bridge).
/// </summary>
public static class TurntableSnapRange
{
    /// <summary>Tap assist only engages when this close to the nearest lock angle.</summary>
    public const float MaxLockArcMeters = 2f;

    /// <summary>
    /// Arc length at the bridge end for an angle error.
    /// <paramref name="bridgeHalfLengthMeters"/> is <c>curve.length / 2</c> (DV SearchRadius).
    /// </summary>
    public static float ArcMeters(float angleDeltaDegrees, float bridgeHalfLengthMeters)
    {
        if (bridgeHalfLengthMeters <= 0f)
        {
            return float.MaxValue;
        }

        return Math.Abs(angleDeltaDegrees) * (float)(Math.PI / 180.0) * bridgeHalfLengthMeters;
    }

    public static bool IsWithinLockArc(
        float angleDeltaDegrees,
        float bridgeHalfLengthMeters,
        float maxArcMeters = MaxLockArcMeters) =>
        ArcMeters(angleDeltaDegrees, bridgeHalfLengthMeters) <= maxArcMeters;
}
