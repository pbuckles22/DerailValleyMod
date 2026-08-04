using System;
using System.Collections.Generic;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>Per-track planning inputs for Align Route debug (3.5).</summary>
public readonly struct PathTrackMeta
{
    public PathTrackMeta(float lengthMeters, float? geometryLimitKmh, PathTrackClass trackClass)
    {
        LengthMeters = lengthMeters > 0f ? lengthMeters : PathTrackCosts.MinLengthMeters;
        GeometryLimitKmh = geometryLimitKmh;
        TrackClass = trackClass;
    }

    public float LengthMeters { get; }
    public float? GeometryLimitKmh { get; }
    public PathTrackClass TrackClass { get; }
}

/// <summary>
/// Structured pathfind debug for Player.log — proves what Dijkstra optimized and why.
/// Optimal for the cost model ≠ perfect real-world ETA; use costcheck + yards + spur counts.
/// </summary>
public static class PathRouteDebug
{
    public static string FormatDetail(
        string reason,
        string origin,
        string dest,
        PathPlanResult plan,
        IReadOnlyList<PathEdge> edges,
        Func<string, PathTrackMeta?>? metaFor = null)
    {
        if (plan == null)
        {
            return $"T2 path: detail {reason} (null plan) ({origin} → {dest})";
        }

        Analyze(plan, edges, metaFor, out var hopSum, out var meters, out var spurHops,
            out var junctionHops, out var reverseHops, out var yards);

        var revPen = reverseHops * PathTrackCosts.ReversePenalty;
        var eta = RouteEtaDisplay.Format(plan.TotalCost) ?? "ETA —";
        var avgKmh = AveragePlanningKmh(meters, hopSum, spurHops, junctionHops);
        var yardChip = yards.Count == 0 ? "—" : string.Join(",", yards);
        var corridor = FormatCorridor(plan.TrackIds);

        return "T2 path: detail "
            + reason
            + " tracks=" + plan.TrackIds.Count
            + " cost=" + plan.TotalCost.ToString("0.0") + "s"
            + " " + eta
            + " meters=" + meters.ToString("0")
            + " avgPlan=" + avgKmh.ToString("0") + "km/h"
            + " rev=" + reverseHops
            + " junc=" + junctionHops
            + " spur=" + spurHops
            + " wrong=" + plan.MisalignedCount
            + " yards=" + yardChip
            + " corridor=" + corridor
            + " (" + origin + " → " + dest + ")";
    }

    public static string FormatCostCheck(
        PathPlanResult plan,
        IReadOnlyList<PathEdge> edges,
        Func<string, PathTrackMeta?>? metaFor = null)
    {
        if (plan == null)
        {
            return "T2 path: costcheck (null plan)";
        }

        Analyze(plan, edges, metaFor, out var hopSum, out _, out var spurHops,
            out var junctionHops, out var reverseHops, out _);

        var revPen = reverseHops * PathTrackCosts.ReversePenalty;
        var spurPen = spurHops * PathTrackCosts.SpurOccupancyPenaltySeconds;
        var juncPen = junctionHops * PathTrackCosts.JunctionPenaltySeconds;
        var reconstructed = hopSum + revPen;
        var delta = Math.Abs(reconstructed - plan.TotalCost);
        var ok = delta < 0.6f ? "ok" : "mismatch";

        // junc/spur penalties are already inside hopSum; revPen is added by Dijkstra separately.
        return "T2 path: costcheck hopSum="
            + hopSum.ToString("0.0")
            + "s (incl junc~" + juncPen.ToString("0")
            + "s spur~" + spurPen.ToString("0")
            + "s) revPen=" + revPen.ToString("0")
            + "s total=" + plan.TotalCost.ToString("0.0")
            + "s recon=" + reconstructed.ToString("0.0")
            + "s " + ok
            + " | Dijkstra is optimal for this cost — cross-check yards/spur/meters vs a slower corridor";
    }

    private static void Analyze(
        PathPlanResult plan,
        IReadOnlyList<PathEdge> edges,
        Func<string, PathTrackMeta?>? metaFor,
        out float hopSum,
        out float meters,
        out int spurHops,
        out int junctionHops,
        out int reverseHops,
        out List<string> yards)
    {
        hopSum = 0f;
        meters = 0f;
        spurHops = 0;
        junctionHops = 0;
        reverseHops = 0;
        yards = new List<string>();
        var yardSeen = new HashSet<string>(StringComparer.Ordinal);

        CollectYards(plan.TrackIds, yardSeen, yards);

        if (plan.TrackIds.Count < 2 || edges == null)
        {
            return;
        }

        var adj = BuildAdjacency(edges);
        for (var i = 0; i < plan.TrackIds.Count - 1; i++)
        {
            var from = plan.TrackIds[i]?.Trim();
            var to = plan.TrackIds[i + 1]?.Trim();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (!TryGetHop(adj, from!, to!, out var hop))
            {
                continue;
            }

            hopSum += hop.Cost;
            if (hop.RequiresReverse)
            {
                reverseHops++;
            }

            if (hop.HasJunction)
            {
                junctionHops++;
            }

            PathTrackMeta? meta = metaFor?.Invoke(to!);
            if (meta is PathTrackMeta m)
            {
                meters += m.LengthMeters;
                if (m.TrackClass == PathTrackClass.SpurPocket)
                {
                    spurHops++;
                }
            }
        }
    }

    private static float AveragePlanningKmh(float meters, float hopSum, int spurHops, int junctionHops)
    {
        // Spur / non-through penalties live in Dijkstra total, not edge Cost.
        // Strip junction fixed add-ons baked into TravelSeconds when junctionHop=true.
        _ = spurHops;
        var travel = hopSum - (junctionHops * PathTrackCosts.JunctionPenaltySeconds);
        if (meters <= 1f || travel < 0.5f)
        {
            return 0f;
        }

        return SpeedDisplay.ToKilometersPerHour(meters / travel);
    }

    private static void CollectYards(
        IReadOnlyList<string> trackIds,
        HashSet<string> seen,
        List<string> yards)
    {
        for (var i = 0; i < trackIds.Count; i++)
        {
            var id = trackIds[i]?.Trim();
            if (string.IsNullOrEmpty(id) || id!.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var dash = id.IndexOf('-');
            if (dash < 2 || dash > 4)
            {
                continue;
            }

            var yard = id.Substring(0, dash);
            if (seen.Add(yard))
            {
                yards.Add(yard);
            }
        }
    }

    /// <summary>
    /// Sum Dijkstra edge costs (+ reverse penalties) from the current corridor index to dest.
    /// <paramref name="progressAlongFirstHop"/> (0..1) bleeds off the first hop while still on
    /// that track so ETA counts down on long segments (not only at node changes).
    /// </summary>
    public static float? RemainingCostSeconds(
        PathPlanResult plan,
        int fromTrackIndex,
        IReadOnlyList<PathEdge> edges,
        float progressAlongFirstHop = 0f)
    {
        if (plan == null || edges == null || plan.TrackIds.Count == 0)
        {
            return null;
        }

        if (fromTrackIndex < 0)
        {
            return null;
        }

        if (fromTrackIndex >= plan.TrackIds.Count - 1)
        {
            return 0f;
        }

        var adj = BuildAdjacency(edges);
        var sum = 0f;
        float? firstHopCost = null;
        for (var i = fromTrackIndex; i < plan.TrackIds.Count - 1; i++)
        {
            var from = plan.TrackIds[i]?.Trim();
            var to = plan.TrackIds[i + 1]?.Trim();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (!TryGetHop(adj, from!, to!, out var hop))
            {
                continue;
            }

            var step = hop.Cost;
            if (hop.RequiresReverse)
            {
                step += PathTrackCosts.ReversePenalty;
            }

            if (firstHopCost == null)
            {
                firstHopCost = step;
            }

            sum += step;
        }

        if (firstHopCost is float first && first > 0f)
        {
            var p = progressAlongFirstHop;
            if (p < 0f)
            {
                p = 0f;
            }
            else if (p > 1f)
            {
                p = 1f;
            }

            sum -= first * p;
            if (sum < 0f)
            {
                sum = 0f;
            }
        }

        return sum;
    }

    /// <summary>
    /// Remaining corridor meters from current index: unfinished current track + full later tracks.
    /// </summary>
    public static float? RemainingMeters(
        PathPlanResult plan,
        int fromTrackIndex,
        float progressAlongFirstHop,
        Func<string, PathTrackMeta?>? metaFor)
    {
        if (plan == null || plan.TrackIds.Count == 0 || fromTrackIndex < 0)
        {
            return null;
        }

        if (fromTrackIndex >= plan.TrackIds.Count)
        {
            return 0f;
        }

        var p = progressAlongFirstHop;
        if (p < 0f)
        {
            p = 0f;
        }
        else if (p > 1f)
        {
            p = 1f;
        }

        var sum = 0f;
        var any = false;
        for (var i = fromTrackIndex; i < plan.TrackIds.Count; i++)
        {
            var id = plan.TrackIds[i]?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var meta = metaFor?.Invoke(id!);
            if (meta is not PathTrackMeta m)
            {
                continue;
            }

            any = true;
            var len = m.LengthMeters;
            if (i == fromTrackIndex)
            {
                len *= 1f - p;
            }

            sum += len;
        }

        return any ? sum : null;
    }

    /// <summary>
    /// Live ETA from remaining meters and <b>actual</b> loco speed (km/h). Null when too slow / unknown.
    /// Do not pass speed-limit chip values.
    /// </summary>
    public static float? LiveEtaSeconds(float remainingMeters, float speedKmh, float minSpeedKmh = 8f)
    {
        if (remainingMeters <= 0f || speedKmh < minSpeedKmh)
        {
            return null;
        }

        var mps = SpeedDisplay.ToMetersPerSecond(speedKmh);
        if (mps < 0.01f)
        {
            return null;
        }

        return remainingMeters / mps;
    }

    /// <summary>
    /// Google-style remaining ETA: <c>rem ÷ EMA(speed)</c>. Faster than plan → ETA drops;
    /// slower → rises. Rate-limited so throttle blips do not thrash the chip.
    /// Cold start / crawl uses plan-scaled seconds until a usable pace exists.
    /// </summary>
    public static float PaceEtaSeconds(
        float remainingMeters,
        float planRemainingSeconds,
        float instantSpeedKmh,
        ref float smoothedSpeedKmh,
        ref float previousEtaSeconds,
        float alpha = 0.22f,
        float minSpeedKmh = 8f,
        float maxStepFraction = 0.12f,
        float maxStepSeconds = 20f)
    {
        if (remainingMeters <= 0f)
        {
            previousEtaSeconds = 0f;
            return 0f;
        }

        if (instantSpeedKmh >= minSpeedKmh)
        {
            if (smoothedSpeedKmh < minSpeedKmh)
            {
                smoothedSpeedKmh = instantSpeedKmh;
            }
            else
            {
                smoothedSpeedKmh = alpha * instantSpeedKmh + (1f - alpha) * smoothedSpeedKmh;
            }
        }

        float raw;
        if (smoothedSpeedKmh >= minSpeedKmh)
        {
            var mps = SpeedDisplay.ToMetersPerSecond(smoothedSpeedKmh);
            raw = mps < 0.01f ? planRemainingSeconds : remainingMeters / mps;
        }
        else
        {
            raw = planRemainingSeconds < 0f ? 0f : planRemainingSeconds;
        }

        if (previousEtaSeconds < 0f)
        {
            previousEtaSeconds = raw;
            return raw;
        }

        var maxStep = Math.Max(maxStepSeconds, previousEtaSeconds * maxStepFraction);
        var lo = previousEtaSeconds - maxStep;
        var hi = previousEtaSeconds + maxStep;
        var clamped = raw < lo ? lo : (raw > hi ? hi : raw);
        if (clamped < 0f)
        {
            clamped = 0f;
        }

        previousEtaSeconds = clamped;
        return clamped;
    }

    /// <summary>Overall corridor progress 0..1 from planned vs remaining meters.</summary>
    public static float TripProgress01(float plannedMeters, float remainingMeters)
    {
        if (plannedMeters <= 1f)
        {
            return 0f;
        }

        var done = plannedMeters - remainingMeters;
        if (done <= 0f)
        {
            return 0f;
        }

        if (done >= plannedMeters)
        {
            return 1f;
        }

        return done / plannedMeters;
    }

    /// <summary>
    /// Trip progress from original travel ETA vs remaining travel ETA:
    /// <c>1 − rem/original</c>. Arrival (rem ≤ 0) ⇒ 1.
    /// Pass the <b>displayed</b> remaining ETA (after soft lag), not odometer-scaled plan.
    /// </summary>
    public static float TripProgressFromEta(float originalTravelSeconds, float remainingTravelSeconds)
    {
        if (originalTravelSeconds <= 0.5f)
        {
            return remainingTravelSeconds <= 0f ? 1f : 0f;
        }

        if (remainingTravelSeconds <= 0f)
        {
            return 1f;
        }

        var done = 1f - (remainingTravelSeconds / originalTravelSeconds);
        if (done <= 0f)
        {
            return 0f;
        }

        return done >= 1f ? 1f : done;
    }

    /// <summary>
    /// Remaining meters kept in sync with remaining travel ETA fraction of the planned corridor.
    /// </summary>
    public static float RemainingMetersFromEta(
        float plannedMeters,
        float originalTravelSeconds,
        float remainingTravelSeconds)
    {
        if (plannedMeters <= 0f || remainingTravelSeconds <= 0f)
        {
            return 0f;
        }

        if (originalTravelSeconds <= 0.5f)
        {
            return plannedMeters;
        }

        var frac = remainingTravelSeconds / originalTravelSeconds;
        if (frac <= 0f)
        {
            return 0f;
        }

        if (frac >= 1f)
        {
            return plannedMeters;
        }

        return plannedMeters * frac;
    }

    /// <summary>
    /// True when the loco is on the destination track (arrival — rem/ETA snap to 0).
    /// Does not treat an earlier corridor hop as arrived (avoids rem/ETA dying before park).
    /// </summary>
    public static bool IsAtDestination(
        string? currentTrackId,
        string? destTrackId,
        PathPlanResult? plan)
    {
        _ = plan;
        var cur = currentTrackId?.Trim();
        if (string.IsNullOrEmpty(cur))
        {
            return false;
        }

        var dest = destTrackId?.Trim();
        return !string.IsNullOrEmpty(dest)
            && string.Equals(cur, dest, StringComparison.Ordinal);
    }

    /// <summary>
    /// Rem for HUD/ETA: prefer the longer of odometer rem and graph rem while not at dest.
    /// Floors to 1 m until arrived so trip% / ETA never report "done" early.
    /// </summary>
    public static float EffectiveRemainingMeters(
        float driveRemMeters,
        float? graphRemMeters,
        bool atDestination)
    {
        if (atDestination)
        {
            return 0f;
        }

        var rem = driveRemMeters < 0f ? 0f : driveRemMeters;
        if (graphRemMeters is float g && g > rem)
        {
            rem = g;
        }

        return rem < 1f ? 1f : rem;
    }

    /// <summary>
    /// Earliest corridor index among candidates (more remaining meters — safer ETA floor).
    /// </summary>
    public static int EarliestCorridorIndex(
        IReadOnlyList<string> trackIds,
        IEnumerable<string?>? candidates)
    {
        if (trackIds == null || candidates == null)
        {
            return -1;
        }

        var best = -1;
        foreach (var c in candidates)
        {
            var i = IndexOfTrack(trackIds, c);
            if (i < 0)
            {
                continue;
            }

            if (best < 0 || i < best)
            {
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// Honest trip meters left: planned corridor length minus odometer since Set dest.
    /// Avoids jumping when junction hops clip long RailTrack nodes.
    /// </summary>
    public static float RemainingFromDrive(float plannedMeters, float driveMetersSincePlan)
    {
        if (plannedMeters <= 0f)
        {
            return 0f;
        }

        var rem = plannedMeters - (driveMetersSincePlan < 0f ? 0f : driveMetersSincePlan);
        return rem < 0f ? 0f : rem;
    }

    /// <summary>Scale full plan seconds by drive-based remaining fraction.</summary>
    public static float PlanEtaFromDrive(float planTotalSeconds, float plannedMeters, float remainingMeters)
    {
        if (planTotalSeconds <= 0f || plannedMeters <= 1f)
        {
            return planTotalSeconds < 0f ? 0f : planTotalSeconds;
        }

        return planTotalSeconds * (remainingMeters / plannedMeters);
    }

    public static int IndexOfTrack(IReadOnlyList<string> trackIds, string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id) || trackIds == null)
        {
            return -1;
        }

        for (var i = 0; i < trackIds.Count; i++)
        {
            if (string.Equals(trackIds[i], id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    public static string FormatCorridor(IReadOnlyList<string> trackIds, int head = 4, int tail = 4)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            return "—";
        }

        if (trackIds.Count <= head + tail + 1)
        {
            return string.Join(">", trackIds);
        }

        var sb = new StringBuilder();
        for (var i = 0; i < head; i++)
        {
            if (i > 0)
            {
                sb.Append('>');
            }

            sb.Append(trackIds[i]);
        }

        sb.Append(">…>");
        for (var i = trackIds.Count - tail; i < trackIds.Count; i++)
        {
            if (i > trackIds.Count - tail)
            {
                sb.Append('>');
            }

            sb.Append(trackIds[i]);
        }

        return sb.ToString();
    }

    /// <summary>Short class tag for corridor diagnostics.</summary>
    public static string ClassTag(PathTrackClass trackClass) =>
        trackClass switch
        {
            PathTrackClass.Through => "Thru",
            PathTrackClass.YardService => "Yard",
            PathTrackClass.SpurPocket => "Spur",
            _ => "Unk",
        };

    /// <summary>
    /// Corridor head with per-track class and occupancy mark (<c>*</c>) — proves whether Align
    /// picked a lane the occupancy filter should have removed, or a misclassified free lane.
    /// </summary>
    public static string FormatCorridorMeta(
        IReadOnlyList<string> trackIds,
        Func<string, PathTrackClass>? classOf,
        Func<string, bool>? occupiedOf,
        int head = 8)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            return "—";
        }

        var sb = new StringBuilder();
        var count = Math.Min(head, trackIds.Count);
        for (var i = 0; i < count; i++)
        {
            var id = trackIds[i]?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(id);
            sb.Append(':');
            sb.Append(ClassTag(classOf?.Invoke(id!) ?? PathTrackClass.Unknown));
            if (occupiedOf?.Invoke(id!) == true)
            {
                sb.Append('*');
            }
        }

        if (trackIds.Count > count)
        {
            sb.Append(" …+");
            sb.Append(trackIds.Count - count);
        }

        return sb.Length == 0 ? "—" : sb.ToString();
    }

    /// <summary>Junction cues along the plan: <c>Jid req/act</c> (<c>!</c> when misaligned).</summary>
    public static string FormatJunctionCues(PathPlanResult plan, int head = 8)
    {
        if (plan?.Junctions == null || plan.Junctions.Count == 0)
        {
            return "—";
        }

        var sb = new StringBuilder();
        var count = Math.Min(head, plan.Junctions.Count);
        for (var i = 0; i < count; i++)
        {
            var j = plan.Junctions[i];
            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(j.JunctionId);
            sb.Append(' ');
            sb.Append(j.RequiredBranch);
            sb.Append('/');
            sb.Append(j.ActualBranch);
            if (!j.Aligned)
            {
                sb.Append('!');
            }
        }

        if (plan.Junctions.Count > count)
        {
            sb.Append(" …+");
            sb.Append(plan.Junctions.Count - count);
        }

        return sb.ToString();
    }

    /// <summary>First <paramref name="head"/> keys of a set (occupancy sample for the log).</summary>
    public static string FormatKeySample(IEnumerable<string>? keys, int head = 8)
    {
        if (keys == null)
        {
            return "—";
        }

        var sb = new StringBuilder();
        var shown = 0;
        var total = 0;
        foreach (var key in keys)
        {
            total++;
            if (shown >= head)
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(key);
            shown++;
        }

        if (total == 0)
        {
            return "—";
        }

        if (total > shown)
        {
            sb.Append(" …+");
            sb.Append(total - shown);
        }

        return sb.ToString();
    }

    /// <summary>
    /// What Dijkstra sees at the start: origin/dest yards, origin key candidates.
    /// </summary>
    public static string FormatThinkHeader(
        string reason,
        string origin,
        string dest,
        IReadOnlyList<string>? originCandidates)
    {
        var oy = PathRouteConstraints.YardIdOf(origin) ?? "—";
        var dy = PathRouteConstraints.YardIdOf(dest) ?? "—";
        var cands = originCandidates == null || originCandidates.Count == 0
            ? origin
            : FormatKeySample(originCandidates, head: 8);
        // splice= is the graph key under the loco (often #Y-… when Track HUD chip is blank).
        return "T2 path: think "
            + reason
            + " splice=" + origin
            + " dest=" + dest
            + " oYard=" + oy
            + " dYard=" + dy
            + " originCands=" + cands;
    }

    /// <summary>
    /// Nearby graph keys by world distance — named HB-* plus anonymous #Y so we can match
    /// the rail "straight ahead" to a Dijkstra node.
    /// </summary>
    public static string FormatNearbyTracks(
        string reason,
        string origin,
        IReadOnlyList<(string TrackId, float DistM, PathTrackClass Cls, bool Occupied)> nearby,
        int head = 40)
    {
        var sb = new StringBuilder();
        sb.Append("T2 path: yard-near ");
        sb.Append(reason);
        sb.Append(" at=");
        sb.Append(string.IsNullOrEmpty(origin) ? "—" : origin);
        sb.Append(" n=");
        sb.Append(nearby?.Count ?? 0);
        if (nearby == null || nearby.Count == 0)
        {
            sb.Append(" —");
            return sb.ToString();
        }

        var shown = Math.Min(head, nearby.Count);
        for (var i = 0; i < shown; i++)
        {
            var row = nearby[i];
            sb.Append(" | ");
            sb.Append(row.TrackId);
            sb.Append(':');
            sb.Append(ClassTag(row.Cls));
            if (row.Occupied)
            {
                sb.Append('*');
            }

            sb.Append(' ');
            sb.Append(row.DistM.ToString("0"));
            sb.Append('m');
        }

        if (nearby.Count > shown)
        {
            sb.Append(" | …+");
            sb.Append(nearby.Count - shown);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Two-hop fanout from origin on the raw graph (junction tags) — shows what "straight"
    /// continues as after the first rail.
    /// </summary>
    public static string FormatOriginFanout(
        string reason,
        string origin,
        IReadOnlyList<PathEdge> rawEdges,
        Func<string, PathTrackClass>? classFor,
        ISet<string>? occupied,
        int head = 24)
    {
        if (string.IsNullOrEmpty(origin) || rawEdges == null)
        {
            return "T2 path: fanout " + reason + " —";
        }

        var adj = BuildAdjacency(rawEdges);
        if (!adj.TryGetValue(origin, out var outs) || outs.Count == 0)
        {
            return "T2 path: fanout " + reason + " (no outs from " + origin + ")";
        }

        var sb = new StringBuilder();
        sb.Append("T2 path: fanout ");
        sb.Append(reason);
        sb.Append(" from=");
        sb.Append(origin);
        var shown = 0;
        foreach (var e in outs)
        {
            var to = e.ToTrackId?.Trim();
            if (string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (shown >= head)
            {
                sb.Append(" | …+");
                sb.Append(outs.Count - shown);
                break;
            }

            shown++;
            var cls = classFor?.Invoke(to!) ?? PathTrackClass.Unknown;
            var occ = occupied != null && occupied.Contains(to!);
            sb.Append(" | ");
            sb.Append(to);
            sb.Append(':');
            sb.Append(ClassTag(cls));
            if (occ)
            {
                sb.Append('*');
            }

            if (e.HasJunction)
            {
                sb.Append(" J=");
                sb.Append(e.JunctionId);
                sb.Append(':');
                sb.Append(e.RequiredBranch);
            }
            else
            {
                sb.Append(" plain");
            }

            if (adj.TryGetValue(to!, out var nextHops) && nextHops.Count > 0)
            {
                sb.Append(" ->");
                var nShow = Math.Min(3, nextHops.Count);
                for (var i = 0; i < nShow; i++)
                {
                    var n = nextHops[i];
                    var nt = n.ToTrackId?.Trim();
                    if (string.IsNullOrEmpty(nt))
                    {
                        continue;
                    }

                    sb.Append(' ');
                    sb.Append(nt);
                    if (n.HasJunction)
                    {
                        sb.Append("[J=");
                        sb.Append(n.JunctionId);
                        sb.Append(':');
                        sb.Append(n.RequiredBranch);
                        sb.Append(']');
                    }
                }

                if (nextHops.Count > nShow)
                {
                    sb.Append(" +");
                    sb.Append(nextHops.Count - nShow);
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Per-choice reachability to dest on the filtered graph + stem-skip ON/OFF compare.
    /// Answers: is the straight rail missing, or present but unused / blocked?
    /// </summary>
    public static string FormatReachProbe(
        string reason,
        string origin,
        string dest,
        IReadOnlyList<PathEdge> filteredEdges,
        IReadOnlyList<PathEdge> rawEdges,
        Func<string, PathTrackClass>? classFor)
    {
        var emptySel = new Dictionary<string, int>(StringComparer.Ordinal);
        var sb = new StringBuilder();
        sb.Append("T2 path: reach ");
        sb.Append(reason);

        var stemOn = PathPlan.Find(
            filteredEdges, emptySel, origin, dest, classFor, skipPlainOnMultiBranchStem: true);
        var stemOff = PathPlan.Find(
            filteredEdges, emptySel, origin, dest, classFor, skipPlainOnMultiBranchStem: false);
        sb.Append(" | stemON=");
        AppendReachChip(sb, stemOn);
        sb.Append(" | stemOFF=");
        AppendReachChip(sb, stemOff);

        if (!string.IsNullOrEmpty(origin) && rawEdges != null)
        {
            foreach (var e in rawEdges)
            {
                if (!string.Equals(e.FromTrackId, origin, StringComparison.Ordinal))
                {
                    continue;
                }

                var to = e.ToTrackId?.Trim();
                if (string.IsNullOrEmpty(to))
                {
                    continue;
                }

                var alone = PathPlan.Find(
                    filteredEdges, emptySel, to, dest, classFor, skipPlainOnMultiBranchStem: true);
                sb.Append(" | via=");
                sb.Append(to);
                if (e.HasJunction)
                {
                    sb.Append("[J=");
                    sb.Append(e.JunctionId);
                    sb.Append(':');
                    sb.Append(e.RequiredBranch);
                    sb.Append(']');
                }
                else
                {
                    sb.Append("[plain]");
                }

                sb.Append('=');
                AppendReachChip(sb, alone);
                if (alone.Status != PathCheckStatus.NoPath
                    && alone.Status != PathCheckStatus.NoOrigin
                    && alone.Status != PathCheckStatus.NoDestination
                    && alone.TrackIds.Count > 1)
                {
                    sb.Append(" next=");
                    sb.Append(alone.TrackIds[1]);
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendReachChip(StringBuilder sb, PathPlanResult plan)
    {
        if (plan == null
            || plan.Status == PathCheckStatus.NoPath
            || plan.Status == PathCheckStatus.NoOrigin
            || plan.Status == PathCheckStatus.NoDestination)
        {
            sb.Append("NO");
            return;
        }

        sb.Append("YES cost=");
        sb.Append(plan.TotalCost.ToString("0"));
        sb.Append('s');
        sb.Append(" n=");
        sb.Append(plan.TrackIds.Count);
        if (plan.TrackIds.Count > 1)
        {
            sb.Append(" first=");
            sb.Append(plan.TrackIds[1]);
        }
    }

    /// <summary>
    /// Outbound choices from origin on the <b>raw</b> graph: kept vs filtered, class, step cost.
    /// Sorted cheapest-first so the free lane (if legal) is obvious next to the chosen hop.
    /// </summary>
    public static string FormatOriginChoices(
        string reason,
        string origin,
        string dest,
        IReadOnlyList<PathEdge> rawEdges,
        ISet<string>? occupied,
        Func<string, PathTrackClass>? classFor,
        string? chosenNext = null,
        int head = 10)
    {
        if (string.IsNullOrEmpty(origin) || rawEdges == null)
        {
            return "T2 path: choices " + reason + " —";
        }

        var originYard = PathRouteConstraints.YardIdOf(origin);
        var destYard = PathRouteConstraints.YardIdOf(dest);
        var rows = new List<(float Sort, string Text)>();
        var originOuts = new List<PathEdge>();
        foreach (var e in rawEdges)
        {
            if (string.Equals(e.FromTrackId, origin, StringComparison.Ordinal))
            {
                originOuts.Add(e);
            }
        }

        var originIsStem = PathPlan.IsMultiBranchJunctionStem(originOuts);

        foreach (var e in originOuts)
        {
            var to = e.ToTrackId?.Trim();
            if (string.IsNullOrEmpty(to))
            {
                continue;
            }

            var cls = classFor?.Invoke(to!) ?? PathTrackClass.Unknown;
            var occ = occupied != null && occupied.Contains(to!);
            var blocked = PathRouteConstraints.IsEntryBlocked(to, cls, occupied, origin, dest);
            var stepOk = PathPlan.TryStepCost(
                e, to!, dest, originYard, destYard, classFor, out var step);
            // Mirror Dijkstra: plain hops ignored on multi-branch junction stems.
            var skipPlain = originIsStem && !e.HasJunction;
            string status;
            if (blocked)
            {
                status = "DROP-occ";
            }
            else if (!stepOk)
            {
                status = "DROP-rev";
            }
            else if (skipPlain)
            {
                status = "SKIP-plain";
            }
            else
            {
                status = "keep";
            }

            var pick = string.Equals(to, chosenNext, StringComparison.Ordinal) ? "*" : "";
            var junc = e.HasJunction
                ? (" J=" + e.JunctionId + ":" + e.RequiredBranch)
                : "";
            var rev = e.RequiresReverse ? " rev" : "";
            var text = pick
                + to
                + ":"
                + ClassTag(cls)
                + (occ ? "*" : "")
                + " "
                + status
                + " step="
                + (stepOk ? step.ToString("0.0") : "∞")
                + "s base="
                + e.Cost.ToString("0.0")
                + "s"
                + junc
                + rev;
            var sort = stepOk && !blocked && !skipPlain ? step : 1e9f;
            rows.Add((sort, text));
        }

        if (rows.Count == 0)
        {
            return "T2 path: choices " + reason + " (no outbound from " + origin + ")";
        }

        rows.Sort((a, b) => a.Sort.CompareTo(b.Sort));
        var sb = new StringBuilder();
        sb.Append("T2 path: choices ");
        sb.Append(reason);
        sb.Append(" n=");
        sb.Append(rows.Count);
        var shown = Math.Min(head, rows.Count);
        for (var i = 0; i < shown; i++)
        {
            sb.Append(" | ");
            sb.Append(rows[i].Text);
        }

        if (rows.Count > shown)
        {
            sb.Append(" | …+");
            sb.Append(rows.Count - shown);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Chosen corridor hop-by-hop with Dijkstra step cost (base + class/reverse penalties).
    /// </summary>
    public static string FormatHopThink(
        string reason,
        PathPlanResult plan,
        string dest,
        IReadOnlyList<PathEdge> edges,
        Func<string, PathTrackClass>? classFor,
        int head = 10)
    {
        if (plan == null || plan.TrackIds.Count < 2 || edges == null)
        {
            return "T2 path: hops " + reason + " —";
        }

        var adj = BuildAdjacency(edges);
        var origin = plan.TrackIds[0];
        var originYard = PathRouteConstraints.YardIdOf(origin);
        var destYard = PathRouteConstraints.YardIdOf(dest);
        var sb = new StringBuilder();
        sb.Append("T2 path: hops ");
        sb.Append(reason);
        var count = Math.Min(head, plan.TrackIds.Count - 1);
        for (var i = 0; i < count; i++)
        {
            var from = plan.TrackIds[i];
            var to = plan.TrackIds[i + 1];
            if (!TryGetHop(adj, from, to, out var hop))
            {
                sb.Append(" | ");
                sb.Append(from);
                sb.Append('→');
                sb.Append(to);
                sb.Append(":?");
                continue;
            }

            PathPlan.TryStepCost(hop, to, dest, originYard, destYard, classFor, out var step);
            var cls = classFor?.Invoke(to) ?? PathTrackClass.Unknown;
            var extra = step - hop.Cost;
            sb.Append(" | ");
            sb.Append(i + 1);
            sb.Append(':');
            sb.Append(to);
            sb.Append('/');
            sb.Append(ClassTag(cls));
            sb.Append(" step=");
            sb.Append(step.ToString("0.0"));
            sb.Append("s(base=");
            sb.Append(hop.Cost.ToString("0.0"));
            if (extra > 0.05f)
            {
                sb.Append("+pen");
                sb.Append(extra.ToString("0.0"));
            }

            sb.Append(')');
            if (hop.HasJunction)
            {
                sb.Append(" J=");
                sb.Append(hop.JunctionId);
                sb.Append(':');
                sb.Append(hop.RequiredBranch);
            }

            if (hop.RequiresReverse)
            {
                sb.Append(" rev");
            }
        }

        if (plan.TrackIds.Count - 1 > count)
        {
            sb.Append(" | …+");
            sb.Append(plan.TrackIds.Count - 1 - count);
        }

        return sb.ToString();
    }

    private static Dictionary<string, List<PathEdge>> BuildAdjacency(IReadOnlyList<PathEdge> edges)
    {
        var adj = new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            var from = edge.FromTrackId?.Trim();
            if (string.IsNullOrEmpty(from))
            {
                continue;
            }

            if (!adj.TryGetValue(from!, out var list))
            {
                list = new List<PathEdge>();
                adj[from!] = list;
            }

            list.Add(edge);
        }

        return adj;
    }

    private static bool TryGetHop(
        Dictionary<string, List<PathEdge>> adj,
        string from,
        string to,
        out PathEdge hop)
    {
        hop = default;
        if (!adj.TryGetValue(from, out var hops))
        {
            return false;
        }

        PathEdge? plain = null;
        foreach (var candidate in hops)
        {
            if (!string.Equals(candidate.ToTrackId, to, StringComparison.Ordinal))
            {
                continue;
            }

            if (candidate.HasJunction)
            {
                hop = candidate;
                return true;
            }

            plain ??= candidate;
        }

        if (plain == null)
        {
            return false;
        }

        hop = plain.Value;
        return true;
    }
}
