namespace YardMasterSuite.Core;

/// <summary>Which extremities count for switch / zone clearance.</summary>
public enum ConsistClearanceMode
{
    /// <summary>Whole train (loco + cars) must clear the switch.</summary>
    FullTrain = 0,
    /// <summary>Freight cars only (exclude loco) — final delivery / load spot.</summary>
    CarsOnly = 1,
}

/// <summary>Consist vs switch / zone gate.</summary>
public enum ConsistClearanceStatus
{
    Unknown = 0,
    /// <summary>Still fouling — do not throw / not Arrived.</summary>
    Fouling = 1,
    /// <summary>Safe to throw or treat step as arrived.</summary>
    Cleared = 2,
}

/// <summary>
/// Direction-aware consist clearance (reusable for Align / reverse-into / Switch List).
/// </summary>
public static class ConsistSwitchClearance
{
    public const float DefaultMarginMeters = 2f;

    public static ConsistClearanceMode ModeForStep(SwitchListStepKind kind) =>
        kind == SwitchListStepKind.Delivery ? ConsistClearanceMode.CarsOnly : ConsistClearanceMode.FullTrain;

    /// <summary>
    /// Safe to throw: train does not straddle the switch (entirely before or entirely past).
    /// Axis = travel XZ (or any approach axis).
    /// </summary>
    public static ConsistClearanceStatus EvaluateNotOccupying(
        float switchX,
        float switchZ,
        float tipAx,
        float tipAz,
        float tipBx,
        float tipBz,
        float axisX,
        float axisZ,
        float marginMeters = DefaultMarginMeters)
    {
        if (!TryProjections(
                switchX,
                switchZ,
                tipAx,
                tipAz,
                tipBx,
                tipBz,
                axisX,
                axisZ,
                out var a,
                out var b))
        {
            return ConsistClearanceStatus.Unknown;
        }

        var min = a < b ? a : b;
        var max = a > b ? a : b;
        // Straddle: one tip still behind, other already ahead.
        if (min < -marginMeters && max > marginMeters)
        {
            return ConsistClearanceStatus.Fouling;
        }

        return ConsistClearanceStatus.Cleared;
    }

    /// <summary>
    /// Arrived / past gate: entire span is past the switch along travel (trailing tip ≥ margin).
    /// Forward vs reverse is encoded in the travel vector.
    /// </summary>
    public static ConsistClearanceStatus EvaluatePastSwitch(
        float switchX,
        float switchZ,
        float tipAx,
        float tipAz,
        float tipBx,
        float tipBz,
        float travelX,
        float travelZ,
        float marginMeters = DefaultMarginMeters)
    {
        if (!TryProjections(
                switchX,
                switchZ,
                tipAx,
                tipAz,
                tipBx,
                tipBz,
                travelX,
                travelZ,
                out var a,
                out var b))
        {
            return ConsistClearanceStatus.Unknown;
        }

        var trailing = a < b ? a : b;
        return trailing >= marginMeters
            ? ConsistClearanceStatus.Cleared
            : ConsistClearanceStatus.Fouling;
    }

    /// <summary>Both car tips inside accept radius of zone center (loco ignored by caller).</summary>
    public static ConsistClearanceStatus EvaluateCarsInZone(
        float zoneX,
        float zoneZ,
        float radiusMeters,
        float carTipAx,
        float carTipAz,
        float carTipBx,
        float carTipBz)
    {
        if (radiusMeters <= 0f || float.IsNaN(radiusMeters))
        {
            return ConsistClearanceStatus.Unknown;
        }

        if (!Within(zoneX, zoneZ, carTipAx, carTipAz, radiusMeters)
            || !Within(zoneX, zoneZ, carTipBx, carTipBz, radiusMeters))
        {
            return ConsistClearanceStatus.Fouling;
        }

        return ConsistClearanceStatus.Cleared;
    }

    public static bool IsArrived(ConsistClearanceStatus status) =>
        status == ConsistClearanceStatus.Cleared;

    public static bool IsSafeToThrow(ConsistClearanceStatus occupancy) =>
        occupancy == ConsistClearanceStatus.Cleared;

    /// <summary>
    /// Arrived at a switch pin: must be past along travel AND within near radius of the pin.
    /// Prevents false Arrived when a nearby origin switch is already "behind" loco forward
    /// while rem to the real pivot is still hundreds of meters.
    /// </summary>
    public static ConsistClearanceStatus CombinePastAndNear(
        ConsistClearanceStatus pastOrZone,
        float pinX,
        float pinZ,
        float refX,
        float refZ,
        float nearRadiusMeters)
    {
        if (pastOrZone != ConsistClearanceStatus.Cleared)
        {
            return pastOrZone;
        }

        if (nearRadiusMeters <= 0f || float.IsNaN(nearRadiusMeters))
        {
            return ConsistClearanceStatus.Unknown;
        }

        if (!Within(pinX, pinZ, refX, refZ, nearRadiusMeters))
        {
            return ConsistClearanceStatus.Fouling;
        }

        return ConsistClearanceStatus.Cleared;
    }

    private static bool TryProjections(
        float switchX,
        float switchZ,
        float tipAx,
        float tipAz,
        float tipBx,
        float tipBz,
        float axisX,
        float axisZ,
        out float a,
        out float b)
    {
        a = b = float.NaN;
        var tLen = (float)System.Math.Sqrt((axisX * axisX) + (axisZ * axisZ));
        if (tLen < 1e-4f || float.IsNaN(tLen))
        {
            return false;
        }

        var ux = axisX / tLen;
        var uz = axisZ / tLen;
        a = ((tipAx - switchX) * ux) + ((tipAz - switchZ) * uz);
        b = ((tipBx - switchX) * ux) + ((tipBz - switchZ) * uz);
        return !float.IsNaN(a) && !float.IsNaN(b);
    }

    private static bool Within(float cx, float cz, float x, float z, float radius)
    {
        var dx = x - cx;
        var dz = z - cz;
        return (dx * dx) + (dz * dz) <= radius * radius;
    }
}
