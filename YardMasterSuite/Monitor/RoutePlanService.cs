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

        var occupiedNamed = PathOccupancyScanner.SnapshotOccupiedTrackKeys();
        // Same RailTrack may be HB-* (car) and #Y-* (edge) — union aliases first.
        var occupiedAliased = PathGraphBuilder.ExpandOccupiedAliases(occupiedNamed);
        // Then paint #Y stubs that only feed occupied named rails (free-lane barrier preserved).
        var occupied = PathRouteConstraints.ExpandOccupiedThroughAnonymous(
            occupiedAliased, edges, origin, dest);
        PathTrackClass ClassFor(string id)
        {
            var meta = PathGraphBuilder.TryGetTrackMeta(id);
            return meta?.TrackClass ?? PathTrackClass.Unknown;
        }

        var yardAliases = PathGraphBuilder.BuildYardAliasMap();
        string? YardFor(string id) => yardAliases.TryGetValue(id, out var yard)
            ? yard
            : PathRouteConstraints.YardIdOf(id);
        var filtered = PathRouteConstraints.FilterEdges(
            edges, ClassFor, occupied, origin, dest, YardFor);
        var originCands = TelemetryReaderOrigin.TryGetCandidates();
        Main.Log(
            "T2 path: occupancy cars="
            + PathOccupancyScanner.LastCarCount
            + " named="
            + PathOccupancyScanner.LastOccupiedTracks
            + " expanded="
            + occupied.Count
            + " edges="
            + edges.Count
            + "→"
            + filtered.Count);
        Main.Log(PathRouteDebug.FormatThinkHeader(reason, origin, dest, originCands));

        // classFor drives spur / non-through penalties + forward-only reverse ban in Dijkstra.
        LogYardProbe(reason, origin, dest, edges, filtered, occupied, ClassFor);
        var plan = PathPlan.Find(filtered, selected, origin, dest, ClassFor);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            // Still dump choices so we can see why every outbound died.
            Main.Log(PathRouteDebug.FormatOriginChoices(
                reason, origin, dest, edges, occupied, ClassFor));
            RoutePlanSession.Clear();
            return "T2 path: no path (no free through / occupancy) (" + origin + " → " + dest + ")";
        }

        plan = WithCorridorFillIns(plan, filtered);
        LogDijkstraThink(reason, origin, dest, plan, edges, occupied, ClassFor);
        RouteMemo.Clear();
        RouteMemo.Put(origin, dest, plan);

        var exit = TryExitCue(plan, origin);
        var travelEta = PathRouteDebug.RemainingCostSeconds(plan, 0, filtered)
            ?? plan.TotalCost;
        RoutePlanSession.SetPlan(plan, origin, exit, travelEta);
        var snap = PathCorridorDrift.CaptureJunctionBranches(plan, selected);
        RoutePlanSession.SetJunctionSnapshot(snap);
        Main.Log(
            "T2 path: junction-freeze "
            + reason
            + " n="
            + snap.Count
            + " status="
            + plan.Status
            + (snap.Count == 0
                ? ""
                : " · "
                  + PathCorridorDrift.FormatJunctionDrift(snap, selected)));
        RoutePlanSession.SetDriveBaseline(TelemetryReader.SessionDriveMeters);
        ResetEtaPace();
        RefreshRemainingEta();
        return FormatPathLines(reason, plan, origin, dest, exit, filtered);
    }

    /// <summary>
    /// After Align throws, refresh junction alignment on the frozen TrackIds (no new pathfind).
    /// </summary>
    public static string ReevaluateAfterAlign(PathPlanResult thrownPlan)
    {
        if (thrownPlan == null || thrownPlan.TrackIds.Count == 0)
        {
            return Compute("post-align");
        }

        if (!PathGraphBuilder.TryBuild(out var edges, out var selected))
        {
            PathGraphBuilder.InvalidateCache();
            if (!PathGraphBuilder.TryBuild(out edges, out selected))
            {
                return "T2 path: post-align no graph";
            }
        }

        PathTrackClass ClassFor(string id)
        {
            var meta = PathGraphBuilder.TryGetTrackMeta(id);
            return meta?.TrackClass ?? PathTrackClass.Unknown;
        }

        var plan = PathPlan.ReevaluateAlong(thrownPlan.TrackIds, edges, selected, ClassFor);
        plan = WithCorridorFillIns(plan, edges);
        var origin = RoutePlanSession.PlannedOriginTrackId
            ?? (thrownPlan.TrackIds.Count > 0 ? thrownPlan.TrackIds[0] : null);
        var dest = RouteDestSession.TrackId ?? thrownPlan.TrackIds[thrownPlan.TrackIds.Count - 1];
        var exit = origin != null ? TryExitCue(plan, origin) : null;
        var travelEta = PathRouteDebug.RemainingCostSeconds(plan, 0, edges)
            ?? plan.TotalCost;
        RoutePlanSession.SetPlan(plan, origin, exit, travelEta);
        var snap = PathCorridorDrift.CaptureJunctionBranches(plan, selected);
        RoutePlanSession.SetJunctionSnapshot(snap);
        Main.Log(
            "T2 path: junction-freeze post-align n="
            + snap.Count
            + " status="
            + plan.Status
            + (snap.Count == 0
                ? ""
                : " · "
                  + PathCorridorDrift.FormatJunctionDrift(snap, selected)));
        RoutePlanSession.SetDriveBaseline(TelemetryReader.SessionDriveMeters);
        ResetEtaPace();
        RouteMemo.Clear();
        if (origin != null && dest != null)
        {
            RouteMemo.Put(origin, dest, plan);
        }

        RefreshRemainingEta();
        return FormatPathLines("post-align", plan, origin ?? "?", dest ?? "?", exit, edges);
    }

    private static string FormatPathLines(
        string reason,
        PathPlanResult plan,
        string origin,
        string dest,
        string? exit,
        System.Collections.Generic.IReadOnlyList<PathEdge> edges)
    {
        var pathChip = PathCheckDisplay.Format(plan.ToCheckResult()) ?? "Path —";
        pathChip = RouteEtaDisplay.WithPathChip(
            pathChip,
            RoutePlanSession.EtaCostSeconds ?? plan.TotalCost,
            RoutePlanSession.RemainingMeters,
            RoutePlanSession.TripProgress01,
            RoutePlanSession.EtaMode) ?? pathChip;
        var facing = RouteFacingDisplay.Format(plan) ?? "Facing —";
        var exitChip = exit ?? "Exit —";
        var summary = $"T2 path: {reason} {pathChip} / {facing} / {exitChip} ({origin} → {dest})";

        // Extra lines for Player.log validation (Main.Log callers already log the return;
        // emit detail/costcheck as sibling lines here).
        PathTrackMeta? Meta(string id) => PathGraphBuilder.TryGetTrackMeta(id);
        Main.Log(PathRouteDebug.FormatDetail(reason, origin, dest, plan, edges, Meta));
        Main.Log(PathRouteDebug.FormatCostCheck(plan, edges, Meta));
        return summary;
    }

    /// <summary>
    /// Yard slice dump: nearby rails + 2-hop fanout + stemON/OFF reachability.
    /// Proves whether the visual straight rail is in the graph or only blocked by Dijkstra.
    /// </summary>
    private static void LogYardProbe(
        string reason,
        string origin,
        string dest,
        System.Collections.Generic.IReadOnlyList<PathEdge> rawEdges,
        System.Collections.Generic.IReadOnlyList<PathEdge> filteredEdges,
        System.Collections.Generic.HashSet<string> occupied,
        System.Func<string, PathTrackClass> classFor)
    {
        Main.Log(PathRouteDebug.FormatOriginFanout(reason, origin, rawEdges, classFor, occupied));
        Main.Log(PathRouteDebug.FormatReachProbe(
            reason, origin, dest, filteredEdges, rawEdges, classFor));

        if (!PathGraphBuilder.TryGetTrackWorldXZ(origin, out var ox, out var oz))
        {
            Main.Log("T2 path: yard-near " + reason + " at=" + origin + " (no world xz)");
            return;
        }

        var near = PathGraphBuilder.CollectNearbyTracks(ox, oz, radiusMeters: 280f, max: 48);
        var rows = new System.Collections.Generic.List<(string, float, PathTrackClass, bool)>(near.Count);
        for (var i = 0; i < near.Count; i++)
        {
            var (id, dist, cls) = near[i];
            rows.Add((id, dist, cls, occupied.Contains(id)));
        }

        Main.Log(PathRouteDebug.FormatNearbyTracks(reason, origin, rows));
    }

    /// <summary>
    /// Tier 2 Dijkstra think dump — origin choices, chosen hops + penalties, corridor/junctions.
    /// </summary>
    private static void LogDijkstraThink(
        string reason,
        string origin,
        string dest,
        PathPlanResult plan,
        System.Collections.Generic.IReadOnlyList<PathEdge> rawEdges,
        System.Collections.Generic.HashSet<string> occupied,
        System.Func<string, PathTrackClass> classFor)
    {
        var chosenNext = plan.TrackIds.Count > 1 ? plan.TrackIds[1] : null;
        Main.Log(PathRouteDebug.FormatOriginChoices(
            reason, origin, dest, rawEdges, occupied, classFor, chosenNext));
        Main.Log(PathRouteDebug.FormatHopThink(reason, plan, dest, rawEdges, classFor));
        Main.Log(
            "T2 path: corridor-meta "
            + reason
            + " "
            + PathRouteDebug.FormatCorridorMeta(plan.TrackIds, classFor, occupied.Contains));
        Main.Log(
            "T2 path: junctions "
            + reason
            + " "
            + PathRouteDebug.FormatJunctionCues(plan)
            + " | occSample="
            + PathRouteDebug.FormatKeySample(occupied));
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
        ResetEtaPace();
    }

    /// <summary>
    /// Stale when the player leaves the planned corridor, or (Path OK only) when a
    /// corridor switch's live branch no longer matches the frozen required branch.
    /// Cold / partial graph reads do not invent throws.
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

        // Need live edges + junction branches for switch invalidation and fill-ins.
        // Cold map ⇒ skip switch watch (do not MarkStale on unreadable state).
        System.Collections.Generic.IReadOnlyList<PathEdge>? edges = null;
        System.Collections.Generic.Dictionary<string, int>? selected = null;
        var graphReady = PathGraphBuilder.TryBuild(out var built, out var liveSelected);
        if (graphReady)
        {
            edges = built;
            selected = liveSelected;
        }

        // PRODUCT: only Path OK watches throws. Misaligned already shows Path N wrong;
        // Align mid-flight must not MarkStale while Switch() settles.
        if (graphReady
            && PathCorridorDrift.ShouldWatchJunctionDrift(plan.Status)
            && PathCorridorDrift.PlannedJunctionChanged(
                RoutePlanSession.JunctionSnapshot,
                selected))
        {
            var drift = PathCorridorDrift.FormatJunctionDrift(
                RoutePlanSession.JunctionSnapshot,
                selected);
            RoutePlanSession.MarkStale("planned switch changed — Recheck or Align again");
            return "T2 path: planned switch changed (Recheck or Align) · " + drift;
        }

        var candidates = TelemetryReaderOrigin.TryGetCandidates();
        if (candidates.Count == 0)
        {
            return null;
        }

        foreach (var current in candidates)
        {
            if (PathGraphBuilder.IsOnPlannedCorridor(plan, current))
            {
                return null;
            }

            if (edges != null && PathCorridorDrift.IsOnRoute(plan.TrackIds, current, edges))
            {
                return null;
            }
        }

        var joined = string.Join(",", candidates);
        RoutePlanSession.MarkStale("left planned path — Recheck or Align again");
        return "T2 path: left planned path (Recheck or Align) at=" + joined;
    }

    /// <summary>Bake short connector rails into TrackIds so Path OK survives bogie hops Dijkstra skipped.</summary>
    private static PathPlanResult WithCorridorFillIns(
        PathPlanResult plan,
        System.Collections.Generic.IReadOnlyList<PathEdge> edges)
    {
        if (plan.TrackIds.Count < 2 || edges == null)
        {
            return plan;
        }

        var expanded = PathCorridorDrift.ExpandFillIns(plan.TrackIds, edges);
        if (expanded.Count == plan.TrackIds.Count)
        {
            return plan;
        }

        return new PathPlanResult(
            plan.Status,
            expanded,
            plan.Junctions,
            plan.MisalignedCount,
            plan.ReverseCount,
            plan.LastHopRequiresReverse,
            plan.TotalCost);
    }

    private static float _etaLogAt = -999f;
    private static float _etaLogTrip = -1f;
    private static float _etaScheduleLag;
    private static float _etaArrivalUnscaled = -1f;
    private static float _etaPrevPlanRem = -1f;
    private static float _etaDisplayedRem = -1f;

    /// <summary>Clear schedule-lag ETA state when a new plan is frozen.</summary>
    public static void ResetEtaPace()
    {
        RouteEtaSmooth.Reset(
            ref _etaScheduleLag,
            ref _etaArrivalUnscaled,
            ref _etaPrevPlanRem,
            ref _etaDisplayedRem);
        _etaLogAt = -999f;
        _etaLogTrip = -1f;
    }

    /// <summary>
    /// Google-style ETA (~1 s): rem from Drive; seconds = physical corridor travel time
    /// (length / segment speed + switches), scaled by progress + soft schedule lag;
    /// arrival clamped (~12 s/tick) while moving. Stopped/crawl freezes the chip (no wall countdown).
    /// Pace rem÷speed is a lag hint only — not the chip.
    /// No Dijkstra; graph only to seed PlannedMeters once.
    /// </summary>
    public static string? RefreshRemainingEta()
    {
        if (!RoutePlanSession.HasPlan)
        {
            return null;
        }

        var plan = RoutePlanSession.Plan;
        if (plan == null || plan.TrackIds.Count == 0)
        {
            return null;
        }

        float tot;
        if (RoutePlanSession.PlannedMeters is float cached && cached > 1f)
        {
            tot = cached;
        }
        else
        {
            // One-time corridor length from graph meta (cache hit after first Insert/Set dest).
            if (!PathGraphBuilder.TryBuild(out _, out _))
            {
                return null;
            }

            PathTrackMeta? Meta(string id) => PathGraphBuilder.TryGetTrackMeta(id);
            var plannedM = PathRouteDebug.RemainingMeters(plan, 0, 0f, Meta);
            if (plannedM is not float seeded || seeded <= 1f)
            {
                return null;
            }

            tot = seeded;
        }

        var driveBase = RoutePlanSession.DriveMetersAtPlan ?? TelemetryReader.SessionDriveMeters;
        var driveSince = TelemetryReader.SessionDriveMeters - driveBase;
        if (driveSince < 0f)
        {
            driveSince = 0f;
        }

        var at = TelemetryReaderOrigin.TryGet();
        var dest = RouteDestSession.TrackId
            ?? (plan.TrackIds.Count > 0 ? plan.TrackIds[plan.TrackIds.Count - 1] : null);
        // Alias-aware arrival: FullID / FullDisplayID of the same RailTrack as dest.
        var arrived = PathRouteDebug.IsAtDestination(at, dest, plan)
            || (!string.IsNullOrEmpty(at)
                && !string.IsNullOrEmpty(dest)
                && PathGraphBuilder.ExpandOccupiedAliases(new[] { dest! }).Contains(at!));

        // Dijkstra TotalCost includes lane-choice penalties. Those choose the route but are
        // not travel time. Once the corridor is frozen/aligned, ETA uses physical hop time.
        var fullTravelSec = RoutePlanSession.PlannedTravelSeconds ?? plan.TotalCost;
        float driveRemM;
        float planSec;
        if (arrived)
        {
            driveRemM = 0f;
            planSec = 0f;
        }
        else
        {
            // Odometer seeds physical plan remaining; soft lag then forms displayed ETA.
            driveRemM = PathRouteDebug.RemainingFromDrive(tot, driveSince);
            planSec = PathRouteDebug.PlanEtaFromDrive(fullTravelSec, tot, driveRemM);
        }

        var hop = arrived ? 1f : (RoutePlanSession.HopProgress01 ?? 0f);

        var speedMps = TelemetryReader.TryGetAbsSpeedMetersPerSecond();
        var speedKmh = speedMps is float mps
            ? SpeedDisplay.ToKilometersPerHour(mps)
            : 0f;
        // Pace hint uses drive-based rem so speed÷distance stays honest before lag tick.
        var paceHint = arrived ? (float?)0f : RouteEtaSmooth.PaceHintSeconds(driveRemM, speedKmh);
        var now = UnityEngine.Time.unscaledTime;
        var etaSec = arrived
            ? 0f
            : RouteEtaSmooth.Tick(
                planSec,
                paceHint,
                now,
                ref _etaScheduleLag,
                ref _etaArrivalUnscaled,
                ref _etaPrevPlanRem,
                ref _etaDisplayedRem);
        if (arrived)
        {
            _etaDisplayedRem = 0f;
            _etaPrevPlanRem = 0f;
            _etaScheduleLag = 0f;
            _etaArrivalUnscaled = now;
        }

        // Trip% + rem follow displayed ETA (original vs current), not odometer alone.
        var tripProg = arrived
            ? 1f
            : PathRouteDebug.TripProgressFromEta(fullTravelSec, etaSec);
        var remM = arrived
            ? 0f
            : PathRouteDebug.RemainingMetersFromEta(tot, fullTravelSec, etaSec);

        RoutePlanSession.SetRemainingEta(etaSec, remM, tot, tripProg, hop, arrived ? "arrived" : "lag");

        // Log throttle: trip% change ≥1 or every 30 s (avoid Player.log hitch spam).
        var tripDelta = System.Math.Abs(tripProg - _etaLogTrip);
        if (tripDelta < 0.01f && now - _etaLogAt < 30f)
        {
            return null;
        }

        _etaLogAt = now;
        _etaLogTrip = tripProg;

        var mode = arrived ? "arrived" : "lag";
        var chip = RouteEtaDisplay.WithPathChip("Path", etaSec, remM, tripProg, mode);
        return "T2 path: eta-refresh "
            + (chip ?? ("ETA " + etaSec.ToString("0") + "s"))
            + " plan=" + planSec.ToString("0") + "s"
            + " fullTravel=" + fullTravelSec.ToString("0") + "s"
            + " routeScore=" + plan.TotalCost.ToString("0") + "s"
            + " lag=" + _etaScheduleLag.ToString("0") + "s"
            + " hint=" + (paceHint?.ToString("0") ?? "—") + "s"
            + " at=" + (at ?? "—")
            + " drive=" + driveSince.ToString("0") + "m"
            + " spd=" + speedKmh.ToString("0") + "km/h"
            + (arrived ? " arrived" : "");
    }
}
