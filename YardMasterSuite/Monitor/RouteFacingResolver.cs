using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Unity wire for drive-set ("Set Forward" / "Set Reverse") and Exit cues.
/// Cab forward + first AR pin (or dest track) — not topological ReverseCount.
/// </summary>
internal static class RouteFacingResolver
{
    /// <summary>True if the first AR pin (or final dest) is behind the cab.</summary>
    public static bool IsTargetBehind(PathPlanResult? plan)
    {
        if (plan == null
            || !TryGetLoco(out var fwdX, out var fwdZ, out var posX, out var posZ)
            || !TryGetTargetPos(plan, out var tx, out _, out var tz))
        {
            return false;
        }

        return DriveSetFacing.IsTargetBehind(fwdX, fwdZ, tx - posX, tz - posZ);
    }

    /// <summary>Compass exit from loco to the AR pin (or dest), not track centroids.</summary>
    public static string? TryGetExitCue(PathPlanResult? plan)
    {
        if (plan == null
            || !TryGetLoco(out _, out _, out var posX, out var posZ)
            || !TryGetTargetPos(plan, out var tx, out _, out var tz))
        {
            return null;
        }

        return RouteExitDisplay.Format(posX, posZ, tx, tz);
    }

    private static bool TryGetLoco(out float fwdX, out float fwdZ, out float posX, out float posZ)
    {
        fwdX = fwdZ = posX = posZ = 0f;
        try
        {
            var loco = PlayerManager.Car ?? PlayerManager.LastLoco;
            if (loco == null)
            {
                return false;
            }

            var t = loco.transform;
            var fwd = t.forward;
            var pos = t.position;
            fwdX = fwd.x;
            fwdZ = fwd.z;
            posX = pos.x;
            posZ = pos.z;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetTargetPos(PathPlanResult plan, out float x, out float y, out float z)
    {
        x = y = z = 0f;

        var pinId = SwitchListRouteLeg.PickPinJunctionId(plan);
        if (!string.IsNullOrEmpty(pinId)
            && PathGraphBuilder.TryGetJunctionWorldPosition(pinId, out x, out y, out z))
        {
            return true;
        }

        if (plan.TrackIds.Count == 0)
        {
            return false;
        }

        var destId = plan.TrackIds[plan.TrackIds.Count - 1];
        if (!PathGraphBuilder.TryGetRailTrack(destId, out var rail) || rail == null)
        {
            return false;
        }

        try
        {
            var p = rail.transform.position;
            x = p.x;
            y = p.y;
            z = p.z;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
