using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Planner profile on one shared graph.
/// World = long-haul (junction commitment hard-skip). Yard = in-town / TT.
/// </summary>
public enum PathPlanMode
{
    World = 0,
    Yard = 1,
}

/// <summary>
/// First place a Yard corridor re-uses a junction with a different required branch.
/// Pin / list stop = this switch (approach), not the first corridor flip.
/// </summary>
public readonly struct PathJunctionFirstStop
{
    public PathJunctionFirstStop(
        string junctionId,
        int requiredBranch,
        string fromTrackId,
        string toTrackId)
    {
        JunctionId = junctionId ?? string.Empty;
        RequiredBranch = requiredBranch;
        FromTrackId = fromTrackId ?? string.Empty;
        ToTrackId = toTrackId ?? string.Empty;
    }

    public string JunctionId { get; }
    public int RequiredBranch { get; }
    public string FromTrackId { get; }
    public string ToTrackId { get; }
}

/// <summary>Dijkstra path plan with reverse cues for Align Route (3.5).</summary>
public sealed class PathPlanResult
{
    public PathPlanResult(
        PathCheckStatus status,
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathJunctionEval> junctions,
        int misalignedCount,
        int reverseCount,
        bool lastHopRequiresReverse,
        float totalCost,
        PathJunctionFirstStop? junctionFirstStop = null)
    {
        Status = status;
        TrackIds = trackIds;
        Junctions = junctions;
        MisalignedCount = misalignedCount;
        ReverseCount = reverseCount;
        LastHopRequiresReverse = lastHopRequiresReverse;
        TotalCost = totalCost;
        JunctionFirstStop = junctionFirstStop;
    }

    public PathCheckStatus Status { get; }
    public IReadOnlyList<string> TrackIds { get; }
    public IReadOnlyList<PathJunctionEval> Junctions { get; }
    public int MisalignedCount { get; }
    public int ReverseCount { get; }
    public bool LastHopRequiresReverse { get; }
    public float TotalCost { get; }

    /// <summary>
    /// When set, AR pin prefers this approach switch over the first RequiredFlips entry.
    /// </summary>
    public PathJunctionFirstStop? JunctionFirstStop { get; }

    public PathCheckResult ToCheckResult() =>
        new(Status, TrackIds, Junctions, MisalignedCount);

    /// <summary>True when <paramref name="trackId"/> is any hop on this plan (driving along route).</summary>
    public bool ContainsTrack(string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (var i = 0; i < TrackIds.Count; i++)
        {
            if (string.Equals(TrackIds[i], id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>Cost-aware pathfinder used by Align Route preview / throw (3.5).</summary>
public static class PathPlan
{
    public static PathPlanResult Find(
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> junctionSelectedBranch,
        string? originTrackId,
        string? destinationTrackId,
        Func<string, PathTrackClass>? classFor = null,
        bool skipPlainOnMultiBranchStem = true,
        string? destYardId = null,
        Func<string, string?>? yardFor = null,
        PathPlanMode mode = PathPlanMode.World)
    {
        var dest = Normalize(destinationTrackId);
        if (dest == null)
        {
            return Empty(PathCheckStatus.NoDestination);
        }

        var origin = Normalize(originTrackId);
        if (origin == null)
        {
            return Empty(PathCheckStatus.NoOrigin);
        }

        if (string.Equals(origin, dest, StringComparison.Ordinal))
        {
            return new PathPlanResult(
                PathCheckStatus.Aligned,
                new[] { origin },
                Array.Empty<PathJunctionEval>(),
                0,
                0,
                false,
                0f);
        }

        var adj = BuildAdjacency(edges);
        if (!TryDijkstra(
                adj,
                origin,
                dest,
                classFor,
                skipPlainOnMultiBranchStem,
                destYardId,
                yardFor,
                mode,
                out var path,
                out var totalCost))
        {
            return Empty(PathCheckStatus.NoPath);
        }

        var junctionEvals = new List<PathJunctionEval>();
        var seenJunctions = new HashSet<string>(StringComparer.Ordinal);
        var misaligned = 0;
        var reverseCount = 0;
        var lastReverse = false;
        var selected = junctionSelectedBranch ?? new Dictionary<string, int>();

        for (var i = 0; i < path.Count - 1; i++)
        {
            var from = path[i];
            var to = path[i + 1];
            if (!TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (hop.RequiresReverse)
            {
                reverseCount++;
                if (i == path.Count - 2)
                {
                    lastReverse = true;
                }
            }

            AddUniqueJunctionEval(junctionEvals, seenJunctions, ref misaligned, hop, selected);
        }

        TryFindJunctionFirstStop(path, adj, out var firstStop);
        var status = misaligned == 0 ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned;
        return new PathPlanResult(
            status,
            path,
            junctionEvals,
            misaligned,
            reverseCount,
            lastReverse,
            totalCost,
            firstStop);
    }

    /// <summary>
    /// Walk corridor hops; when a junction is required at a different branch than an
    /// earlier hop committed, that switch is the junction-first stop (approach pin).
    /// </summary>
    private static bool TryFindJunctionFirstStop(
        IReadOnlyList<string> trackIds,
        Dictionary<string, List<PathEdge>> adj,
        out PathJunctionFirstStop? stop)
    {
        stop = null;
        if (trackIds == null || trackIds.Count < 2 || adj == null)
        {
            return false;
        }

        var committed = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var from = Normalize(trackIds[i]);
            var to = Normalize(trackIds[i + 1]);
            if (from == null || to == null || !TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (!hop.HasJunction || hop.JunctionId == null)
            {
                continue;
            }

            if (committed.TryGetValue(hop.JunctionId, out var prior)
                && prior != hop.RequiredBranch)
            {
                stop = new PathJunctionFirstStop(
                    hop.JunctionId, hop.RequiredBranch, from, to);
                return true;
            }

            committed[hop.JunctionId] = hop.RequiredBranch;
        }

        return false;
    }

    /// <summary>
    /// Junction flips still needed before the path is clear.
    /// One throw per junction (first required branch along the corridor) — Align cannot
    /// set two branches on the same points.
    /// </summary>
    public static IReadOnlyList<PathJunctionEval> RequiredFlips(PathPlanResult plan)
    {
        if (plan == null || plan.Junctions.Count == 0)
        {
            return Array.Empty<PathJunctionEval>();
        }

        var list = new List<PathJunctionEval>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var j in plan.Junctions)
        {
            if (j.Aligned || string.IsNullOrEmpty(j.JunctionId))
            {
                continue;
            }

            if (!seen.Add(j.JunctionId))
            {
                continue;
            }

            list.Add(j);
        }

        return list;
    }

    /// <summary>
    /// Re-check junction alignment along a frozen corridor (post-Align) without re-Dijkstra.
    /// Preserves TrackIds / reverse cues from the plan that was thrown.
    /// </summary>
    public static PathPlanResult ReevaluateAlong(
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> junctionSelectedBranch,
        Func<string, PathTrackClass>? classFor = null)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            return Empty(PathCheckStatus.NoPath);
        }

        if (trackIds.Count == 1)
        {
            return new PathPlanResult(
                PathCheckStatus.Aligned,
                trackIds,
                Array.Empty<PathJunctionEval>(),
                0,
                0,
                false,
                0f);
        }

        var adj = BuildAdjacency(edges);
        var selected = junctionSelectedBranch ?? new Dictionary<string, int>();
        var junctionEvals = new List<PathJunctionEval>();
        var seenJunctions = new HashSet<string>(StringComparer.Ordinal);
        var misaligned = 0;
        var reverseCount = 0;
        var lastReverse = false;
        var totalCost = 0f;

        var origin = Normalize(trackIds[0]);
        var dest = Normalize(trackIds[trackIds.Count - 1]) ?? string.Empty;
        var originYard = PathRouteConstraints.YardIdOf(origin);
        var destYard = PathRouteConstraints.YardIdOf(dest);

        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var from = Normalize(trackIds[i]);
            var to = Normalize(trackIds[i + 1]);
            if (from == null || to == null || !TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (!TryStepCost(hop, to, dest, originYard, destYard, classFor, out var step))
            {
                // Corridor became illegal under forward-only rules — keep structure, omit cost.
                step = hop.Cost;
            }

            totalCost += step;
            if (hop.RequiresReverse)
            {
                reverseCount++;
                if (i == trackIds.Count - 2)
                {
                    lastReverse = true;
                }
            }

            // First required branch along the corridor wins — dual W-0416:0 then :1
            // must not leave Align flipping the same points forever.
            AddUniqueJunctionEval(junctionEvals, seenJunctions, ref misaligned, hop, selected);
        }

        TryFindJunctionFirstStop(trackIds, adj, out var firstStop);
        var status = misaligned == 0 ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned;
        return new PathPlanResult(
            status,
            trackIds,
            junctionEvals,
            misaligned,
            reverseCount,
            lastReverse,
            totalCost,
            firstStop);
    }

    /// <summary>
    /// One eval per junction id (first hop along the corridor). Later hops that
    /// re-tag the same points with another branch are ignored for Align/HUD.
    /// </summary>
    private static void AddUniqueJunctionEval(
        List<PathJunctionEval> junctionEvals,
        HashSet<string> seenJunctions,
        ref int misaligned,
        PathEdge hop,
        IReadOnlyDictionary<string, int> selected)
    {
        if (!hop.HasJunction || hop.JunctionId == null || !seenJunctions.Add(hop.JunctionId))
        {
            return;
        }

        selected.TryGetValue(hop.JunctionId, out var actual);
        var eval = new PathJunctionEval(hop.JunctionId, hop.RequiredBranch, actual);
        junctionEvals.Add(eval);
        if (!eval.Aligned)
        {
            misaligned++;
        }
    }

    private static PathPlanResult Empty(PathCheckStatus status) =>
        new(status, Array.Empty<string>(), Array.Empty<PathJunctionEval>(), 0, 0, false, 0f);

    private static string? Normalize(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private static Dictionary<string, List<PathEdge>> BuildAdjacency(IReadOnlyList<PathEdge> edges)
    {
        var adj = new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
        if (edges == null)
        {
            return adj;
        }

        foreach (var edge in edges)
        {
            var from = Normalize(edge.FromTrackId);
            var to = Normalize(edge.ToTrackId);
            if (from == null || to == null)
            {
                continue;
            }

            var normalized = new PathEdge(
                from,
                to,
                edge.JunctionId,
                edge.RequiredBranch,
                edge.Cost,
                edge.RequiresReverse);
            if (!adj.TryGetValue(from, out var list))
            {
                list = new List<PathEdge>();
                adj[from] = list;
            }

            list.Add(normalized);
        }

        return adj;
    }

    private static bool TryDijkstra(
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string dest,
        Func<string, PathTrackClass>? classFor,
        bool skipPlainOnMultiBranchStem,
        string? destYardId,
        Func<string, string?>? yardFor,
        PathPlanMode mode,
        out List<string> path,
        out float totalCost)
    {
        path = new List<string>();
        totalCost = 0f;
        var costSoFar = new Dictionary<string, float>(StringComparer.Ordinal) { [origin] = 0f };
        var cameFrom = new Dictionary<string, string>(StringComparer.Ordinal) { [origin] = origin };
        var open = new List<string> { origin };
        string? YardOf(string id) => yardFor?.Invoke(id) ?? PathRouteConstraints.YardIdOf(id);
        var originYard = YardOf(origin);
        var destYard = !string.IsNullOrWhiteSpace(destYardId)
            ? destYardId!.Trim()
            : YardOf(dest);
        var enforceJunctionCommitment = mode != PathPlanMode.Yard;

        while (open.Count > 0)
        {
            var bestIdx = 0;
            var bestCost = costSoFar[open[0]];
            for (var i = 1; i < open.Count; i++)
            {
                var c = costSoFar[open[i]];
                if (c < bestCost)
                {
                    bestCost = c;
                    bestIdx = i;
                }
            }

            var current = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (string.Equals(current, dest, StringComparison.Ordinal))
            {
                path = Reconstruct(cameFrom, origin, dest);
                totalCost = costSoFar[dest];
                return true;
            }

            if (!adj.TryGetValue(current, out var hops))
            {
                continue;
            }

            // At the loco's origin throat, ignore a duplicate plain shortcut so Dijkstra
            // must choose an actual junction branch. Do not repeat this downstream:
            // branch rails can also look like stems, and their plain edge is the continuation.
            var junctionStem = skipPlainOnMultiBranchStem
                && string.Equals(current, origin, StringComparison.Ordinal)
                && IsMultiBranchJunctionStem(hops);

            foreach (var hop in hops)
            {
                if (junctionStem && !hop.HasJunction)
                {
                    continue;
                }

                // World: a single turnout cannot be both 0 and 1 on one corridor (W-0416).
                // Yard: dense #Y mesh breaks cheapest-path substructure — do not hard-skip.
                if (enforceJunctionCommitment
                    && hop.HasJunction
                    && hop.JunctionId != null
                    && ConflictsJunctionCommitment(
                        cameFrom,
                        adj,
                        origin,
                        current,
                        hop.JunctionId,
                        hop.RequiredBranch))
                {
                    continue;
                }

                var next = hop.ToTrackId;
                if (!TryStepCost(
                        hop, next, dest, originYard, destYard, classFor, out var step, yardFor))
                {
                    continue; // forward-only hard ban outside dest / same-town
                }

                var newCost = costSoFar[current] + step;
                if (costSoFar.TryGetValue(next, out var old) && newCost >= old)
                {
                    continue;
                }

                costSoFar[next] = newCost;
                cameFrom[next] = current;
                if (!open.Contains(next))
                {
                    open.Add(next);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// True when the path origin→current already committed <paramref name="junctionId"/>
    /// to a different branch than <paramref name="requiredBranch"/>.
    /// </summary>
    private static bool ConflictsJunctionCommitment(
        Dictionary<string, string> cameFrom,
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string current,
        string junctionId,
        int requiredBranch)
    {
        var node = current;
        var guard = 0;
        while (!string.Equals(node, origin, StringComparison.Ordinal))
        {
            if (!cameFrom.TryGetValue(node, out var prev) || ++guard > 10000)
            {
                return false;
            }

            if (TryGetHop(adj, prev, node, out var prior)
                && prior.HasJunction
                && string.Equals(prior.JunctionId, junctionId, StringComparison.Ordinal)
                && prior.RequiredBranch != requiredBranch)
            {
                return true;
            }

            node = prev;
        }

        return false;
    }

    /// <summary>
    /// True when this node is a turnout stem: 2+ outbound junction hops (different branches).
    /// </summary>
    public static bool IsMultiBranchJunctionStem(IReadOnlyList<PathEdge> hops)
    {
        if (hops == null)
        {
            return false;
        }

        var junctionOuts = 0;
        for (var i = 0; i < hops.Count; i++)
        {
            if (hops[i].HasJunction)
            {
                junctionOuts++;
                if (junctionOuts >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Edge step cost with pass-through rules. Returns false when the hop is hard-banned
    /// (reverse outside destination yard / dest track / same-town). Public for Tier 2 think dumps.
    /// </summary>
    public static bool TryStepCost(
        PathEdge hop,
        string nextTrackId,
        string destTrackId,
        string? originYardId,
        string? destYardId,
        Func<string, PathTrackClass>? classFor,
        out float stepSeconds,
        Func<string, string?>? yardFor = null)
    {
        stepSeconds = hop.Cost;
        var nextYard = yardFor?.Invoke(nextTrackId) ?? PathRouteConstraints.YardIdOf(nextTrackId);
        var inDestYard = nextYard != null
            && destYardId != null
            && string.Equals(nextYard, destYardId, StringComparison.OrdinalIgnoreCase);
        var isDestTrack = string.Equals(nextTrackId, destTrackId, StringComparison.Ordinal);
        var sameTown = originYardId != null
            && destYardId != null
            && string.Equals(originYardId, destYardId, StringComparison.OrdinalIgnoreCase);

        // HARD BAN: reverse only into dest yard / dest track, or any reverse when same-town
        // (Town TT Align — anonymous #Y dest with session yard).
        if (hop.RequiresReverse && !inDestYard && !isDestTrack && !sameTown)
        {
            return false;
        }

        if (classFor != null)
        {
            var toClass = classFor(nextTrackId);
            if (toClass == PathTrackClass.SpurPocket)
            {
                var inOriginYard = nextYard != null
                    && originYardId != null
                    && string.Equals(nextYard, originYardId, StringComparison.OrdinalIgnoreCase);
                if (inDestYard || inOriginYard || isDestTrack)
                {
                    stepSeconds += PathTrackCosts.SpurOccupancyPenaltySeconds * 0.5f;
                }
                else
                {
                    stepSeconds += PathTrackCosts.SpurOccupancyPenaltySeconds;
                }
            }
            else if (toClass == PathTrackClass.Unknown || toClass == PathTrackClass.YardService)
            {
                stepSeconds += PathTrackCosts.NonThroughPenaltySeconds;
            }
        }

        if (hop.RequiresReverse)
        {
            stepSeconds += PathTrackCosts.ReversePenalty;
        }

        return true;
    }

    private static List<string> Reconstruct(
        Dictionary<string, string> cameFrom,
        string origin,
        string dest)
    {
        var path = new List<string>();
        var current = dest;
        path.Add(current);
        while (!string.Equals(current, origin, StringComparison.Ordinal))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
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
