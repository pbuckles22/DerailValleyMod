using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Explicit route compute for Set dest / Recheck / Align (3.5). Never called from HUD tick.
/// </summary>
internal static class RoutePlanService
{
    /// <summary>Compute (or memo-hit) path from current origin to session destination.</summary>
    public static string Compute(string reason)
    {
        if (!RouteDestSession.HasDestination)
        {
            RoutePlanSession.Clear();
            return "T2 path: no destination";
        }

        var dest = RouteDestSession.TrackId!;
        var origin = TelemetryReaderOrigin.TryGet();
        if (origin == null)
        {
            RoutePlanSession.Clear();
            return "T2 path: no origin (stand on a track, or sit in a loco/car)";
        }

        if (!PathGraphBuilder.TryBuild(out var edges, out var selected))
        {
            PathGraphBuilder.InvalidateCache();
            if (!PathGraphBuilder.TryBuild(out edges, out selected))
            {
                RoutePlanSession.Clear();
                return "T2 path: no graph (enter world session)";
            }
        }

        PathPlanResult plan;
        if (RouteMemo.TryGet(origin, dest, out var memo) && memo != null)
        {
            plan = PathPlan.Find(edges, selected, origin, dest);
        }
        else
        {
            plan = PathPlan.Find(edges, selected, origin, dest);
            RouteMemo.Put(origin, dest, plan);
        }

        var exit = TryExitCue(plan, origin);
        RoutePlanSession.SetPlan(plan, origin, exit);
        var pathChip = PathCheckDisplay.Format(plan.ToCheckResult()) ?? "Path —";
        var facing = RouteFacingDisplay.Format(plan) ?? "Facing —";
        var exitChip = exit ?? "Exit —";
        return $"T2 path: {reason} {pathChip} / {facing} / {exitChip} ({origin} → {dest})";
    }

    private static string? TryExitCue(PathPlanResult plan, string origin)
    {
        if (plan.TrackIds.Count < 2)
        {
            return null;
        }

        var next = plan.TrackIds[1];
        if (!PathGraphBuilder.TryGetTrackWorldXZ(origin, out var ox, out var oz))
        {
            return null;
        }

        if (!PathGraphBuilder.TryGetTrackWorldXZ(next, out var nx, out var nz))
        {
            return null;
        }

        return RouteExitDisplay.Format(ox, oz, nx, nz);
    }

    public static void ClearAll()
    {
        RouteDestSession.Clear();
        RoutePlanSession.Clear();
        RouteMemo.Clear();
    }

    /// <summary>
    /// Stale only when the player leaves the planned corridor — not merely the origin
    /// (driving along an aligned path must stay Path OK).
    /// </summary>
    public static string? WatchPathDrift()
    {
        if (!RoutePlanSession.HasPlan && !RoutePlanSession.IsStale)
        {
            return null;
        }

        if (RoutePlanSession.IsStale)
        {
            return null;
        }

        var plan = RoutePlanSession.Plan;
        if (plan == null || plan.TrackIds.Count == 0)
        {
            return null;
        }

        var current = TelemetryReaderOrigin.TryGet();
        if (current == null)
        {
            return null;
        }

        if (plan.ContainsTrack(current))
        {
            return null;
        }

        RoutePlanSession.MarkStale("left planned path — Recheck or Align again");
        return "T2 path: left planned path (Recheck or Align)";
    }
}
