using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure tip / approach helpers for 4.11/4.12 proximity (no Unity).
/// Rear intent = −forward; front intent = loco forward. Cab look ignored.
/// </summary>
public static class BackupProximityAim
{
    /// <summary>Min cos(angle) between tip→target and tip outward for clearance candidates.</summary>
    public const float ApproachConeMinDot = 0.5f;

    /// <summary>
    /// World intent for the rear sensor: always opposite loco forward (cab look ignored).
    /// </summary>
    public static void RearIntent(
        float locoFwdX,
        float locoFwdY,
        float locoFwdZ,
        out float intentX,
        out float intentY,
        out float intentZ) =>
        Normalize(-locoFwdX, -locoFwdY, -locoFwdZ, out intentX, out intentY, out intentZ);

    /// <summary>
    /// World intent for the front sensor: loco forward (cab look ignored).
    /// </summary>
    public static void FrontIntent(
        float locoFwdX,
        float locoFwdY,
        float locoFwdZ,
        out float intentX,
        out float intentY,
        out float intentZ) =>
        Normalize(locoFwdX, locoFwdY, locoFwdZ, out intentX, out intentY, out intentZ);

    /// <summary>How well a tip's outward axis matches intent (−1‥1).</summary>
    public static float TipAlignment(
        float tipOutX,
        float tipOutY,
        float tipOutZ,
        float intentX,
        float intentY,
        float intentZ)
    {
        Normalize(tipOutX, tipOutY, tipOutZ, out var tx, out var ty, out var tz);
        Normalize(intentX, intentY, intentZ, out var ix, out var iy, out var iz);
        return (tx * ix) + (ty * iy) + (tz * iz);
    }

    /// <summary>True when target lies in the tip's forward approach cone.</summary>
    public static bool IsInApproachCone(
        float fromTipToTargetX,
        float fromTipToTargetY,
        float fromTipToTargetZ,
        float tipOutX,
        float tipOutY,
        float tipOutZ,
        float minDot = ApproachConeMinDot)
    {
        var distSq = (fromTipToTargetX * fromTipToTargetX)
            + (fromTipToTargetY * fromTipToTargetY)
            + (fromTipToTargetZ * fromTipToTargetZ);
        if (distSq < 1e-8f)
        {
            return true;
        }

        var inv = 1f / (float)Math.Sqrt(distSq);
        Normalize(tipOutX, tipOutY, tipOutZ, out var ox, out var oy, out var oz);
        var dot = (fromTipToTargetX * inv * ox) + (fromTipToTargetY * inv * oy) + (fromTipToTargetZ * inv * oz);
        return dot >= minDot;
    }

    private static void Normalize(float x, float y, float z, out float nx, out float ny, out float nz)
    {
        var mag = (float)Math.Sqrt((x * x) + (y * y) + (z * z));
        if (mag < 1e-8f)
        {
            nx = 0f;
            ny = 0f;
            nz = 0f;
            return;
        }

        nx = x / mag;
        ny = y / mag;
        nz = z / mag;
    }
}
