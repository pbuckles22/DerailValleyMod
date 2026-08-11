using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Switch CLEARED gate: radial danger circle + through-switch half-plane.
/// Clear side = opposite of the half you entered from (drive through the frog).
/// Pin stays on the switch; rem is meters along the clear axis to the rim.
/// </summary>
public static class RadialSwitchClearance
{
    /// <summary>
    /// Shorten the DE2 baseline circle so the butt clears near the old cab clear spot.
    /// </summary>
    public const float RadiusShortenMeters = 9f;

    /// <summary>
    /// Beyond this distance from the pin, do not show clear-coaching rem (use step · pin range).
    /// </summary>
    public const float CoachNearMeters = 40f;

    /// <summary>Danger-circle radius for the trailing tip (butt).</summary>
    public static float SafeRadiusMeters(float baselineMeters = SwitchListPinClearOffset.De2ClearPastMeters)
    {
        var baseline = baselineMeters > 0f && !float.IsNaN(baselineMeters)
            ? baselineMeters
            : SwitchListPinClearOffset.De2ClearPastMeters;
        var r = baseline - RadiusShortenMeters;
        return r > 1f ? r : 1f;
    }

    /// <summary>
    /// Trailing tip along an axis = lower projection (the "butt" for clearance).
    /// </summary>
    public static void PickTrailingTip(
        float tipAx,
        float tipAz,
        float tipBx,
        float tipBz,
        float axisX,
        float axisZ,
        out float trailX,
        out float trailZ)
    {
        var len = (float)Math.Sqrt((axisX * axisX) + (axisZ * axisZ));
        if (len < 1e-4f || float.IsNaN(len))
        {
            trailX = tipAx;
            trailZ = tipAz;
            return;
        }

        var ux = axisX / len;
        var uz = axisZ / len;
        var a = (tipAx * ux) + (tipAz * uz);
        var b = (tipBx * ux) + (tipBz * uz);
        if (a <= b)
        {
            trailX = tipAx;
            trailZ = tipAz;
        }
        else
        {
            trailX = tipBx;
            trailZ = tipBz;
        }
    }

    /// <summary>
    /// Danger zone = circle radius R around the pin (butt still fouling).
    /// Clear line = butt past that rim on the far side from entry (signedClear ≥ R).
    /// Sticky CLEARED after crossing the line (blow-by OK) until the butt re-enters
    /// the danger circle (coming back onto the switch cancels green).
    /// </summary>
    public static ConsistClearanceStatus EvaluateThroughSwitch(
        float pinX,
        float pinZ,
        float refX,
        float refZ,
        float safeRadius,
        bool wasInside,
        bool stickyCleared,
        float entryDirX,
        float entryDirZ,
        out bool newWasInside,
        out bool newStickyCleared,
        out float newEntryDirX,
        out float newEntryDirZ)
    {
        newEntryDirX = entryDirX;
        newEntryDirZ = entryDirZ;
        newStickyCleared = stickyCleared;

        if (safeRadius <= 0f || float.IsNaN(safeRadius))
        {
            newWasInside = wasInside;
            return ConsistClearanceStatus.Unknown;
        }

        var dx = refX - pinX;
        var dz = refZ - pinZ;
        var distSq = (dx * dx) + (dz * dz);
        var insideNow = distSq < (safeRadius * safeRadius);

        newWasInside = wasInside || insideNow;

        // First entry: remember which half we came from (pin → ref while inside).
        if (insideNow && !wasInside)
        {
            var elen = (float)Math.Sqrt((dx * dx) + (dz * dz));
            if (elen > 1e-4f)
            {
                newEntryDirX = dx / elen;
                newEntryDirZ = dz / elen;
            }
        }

        // Coming back onto the switch (butt in danger) cancels sticky green.
        if (insideNow)
        {
            newStickyCleared = false;
            return ConsistClearanceStatus.Fouling;
        }

        // Past the clear line earlier and still outside danger — blow-by OK.
        if (stickyCleared)
        {
            newStickyCleared = true;
            return ConsistClearanceStatus.Cleared;
        }

        // Never entered the danger circle — cannot have crossed the clear line.
        if (!newWasInside)
        {
            return ConsistClearanceStatus.Fouling;
        }

        var entryLen = (float)Math.Sqrt(
            (newEntryDirX * newEntryDirX) + (newEntryDirZ * newEntryDirZ));
        if (entryLen < 1e-4f || float.IsNaN(entryLen))
        {
            return ConsistClearanceStatus.Fouling;
        }

        // Clear line: butt has moved ≥ R along the far-side axis from the pin.
        var signedClear = SignedClearMeters(
            pinX, pinZ, refX, refZ, newEntryDirX, newEntryDirZ);
        if (signedClear >= safeRadius)
        {
            newStickyCleared = true;
            return ConsistClearanceStatus.Cleared;
        }

        return ConsistClearanceStatus.Fouling;
    }

    /// <summary>
    /// Signed meters along the clear axis (opposite of entry). Positive = toward / past clear rim.
    /// </summary>
    public static float SignedClearMeters(
        float pinX,
        float pinZ,
        float refX,
        float refZ,
        float entryDirX,
        float entryDirZ)
    {
        var len = (float)Math.Sqrt((entryDirX * entryDirX) + (entryDirZ * entryDirZ));
        if (len < 1e-4f || float.IsNaN(len))
        {
            return 0f;
        }

        // clearDir = -entryDir
        var ux = -entryDirX / len;
        var uz = -entryDirZ / len;
        return ((refX - pinX) * ux) + ((refZ - pinZ) * uz);
    }

    /// <summary>
    /// Meters still needed along the clear axis to reach the rim (symmetric coming/going).
    /// 0 when already at/past the clear rim (signed ≥ R).
    /// </summary>
    public static int MetersToClearRimAlongAxis(float signedClearMeters, float safeRadiusMeters)
    {
        if (safeRadiusMeters <= 0f || float.IsNaN(safeRadiusMeters)
            || float.IsNaN(signedClearMeters))
        {
            return 0;
        }

        var rem = safeRadiusMeters - signedClearMeters;
        if (rem <= 0f)
        {
            return 0;
        }

        return (int)Math.Ceiling(rem - 1e-4f);
    }

    public static float DistanceMeters(float pinX, float pinZ, float refX, float refZ)
    {
        var dx = refX - pinX;
        var dz = refZ - pinZ;
        return (float)Math.Sqrt((dx * dx) + (dz * dz));
    }

    public static float DotPinToRef(
        float pinX,
        float pinZ,
        float refX,
        float refZ,
        float dirX,
        float dirZ) =>
        ((refX - pinX) * dirX) + ((refZ - pinZ) * dirZ);
}
