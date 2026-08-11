using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>RouteLeg pin target for active Switch List leg (switch gate, not dest track).</summary>
public static class SwitchListRouteLeg
{
    /// <summary>
    /// Pin target: junction-first stop (Yard dual-branch approach) when present;
    /// else first misaligned junction (RequiredFlips order).
    /// </summary>
    public static string? PickPinJunctionId(PathPlanResult? plan)
    {
        if (plan == null)
        {
            return null;
        }

        if (plan.JunctionFirstStop is PathJunctionFirstStop stop)
        {
            var stopId = stop.JunctionId?.Trim();
            if (!string.IsNullOrEmpty(stopId))
            {
                return stopId;
            }
        }

        var flips = PathPlan.RequiredFlips(plan);
        if (flips.Count == 0)
        {
            return null;
        }

        var id = flips[0].JunctionId?.Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }

    /// <summary>Filter flips to those the consist does not occupy (safe to throw).</summary>
    public static IReadOnlyList<PathJunctionEval> FilterSafeToThrowFlips(
        IReadOnlyList<PathJunctionEval> flips,
        System.Func<string, ConsistClearanceStatus> occupancyForJunctionId)
    {
        if (flips == null || flips.Count == 0 || occupancyForJunctionId == null)
        {
            return System.Array.Empty<PathJunctionEval>();
        }

        var list = new List<PathJunctionEval>(flips.Count);
        for (var i = 0; i < flips.Count; i++)
        {
            var j = flips[i];
            if (string.IsNullOrEmpty(j.JunctionId))
            {
                continue;
            }

            if (ConsistSwitchClearance.IsSafeToThrow(occupancyForJunctionId(j.JunctionId!)))
            {
                list.Add(j);
            }
        }

        return list;
    }
}
