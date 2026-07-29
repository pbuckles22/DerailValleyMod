using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

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
        float totalCost)
    {
        Status = status;
        TrackIds = trackIds;
        Junctions = junctions;
        MisalignedCount = misalignedCount;
        ReverseCount = reverseCount;
        LastHopRequiresReverse = lastHopRequiresReverse;
        TotalCost = totalCost;
    }

    public PathCheckStatus Status { get; }
    public IReadOnlyList<string> TrackIds { get; }
    public IReadOnlyList<PathJunctionEval> Junctions { get; }
    public int MisalignedCount { get; }
    public int ReverseCount { get; }
    public bool LastHopRequiresReverse { get; }
    public float TotalCost { get; }

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
        string? destinationTrackId)
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
        if (!TryDijkstra(adj, origin, dest, out var path, out var totalCost))
        {
            return Empty(PathCheckStatus.NoPath);
        }

        var junctionEvals = new List<PathJunctionEval>();
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

            if (!hop.HasJunction || hop.JunctionId == null)
            {
                continue;
            }

            selected.TryGetValue(hop.JunctionId, out var actual);
            var eval = new PathJunctionEval(hop.JunctionId, hop.RequiredBranch, actual);
            junctionEvals.Add(eval);
            if (!eval.Aligned)
            {
                misaligned++;
            }
        }

        var status = misaligned == 0 ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned;
        return new PathPlanResult(
            status,
            path,
            junctionEvals,
            misaligned,
            reverseCount,
            lastReverse,
            totalCost);
    }

    /// <summary>Junction flips still needed before the path is clear.</summary>
    public static IReadOnlyList<PathJunctionEval> RequiredFlips(PathPlanResult plan)
    {
        if (plan == null || plan.Junctions.Count == 0)
        {
            return Array.Empty<PathJunctionEval>();
        }

        var list = new List<PathJunctionEval>();
        foreach (var j in plan.Junctions)
        {
            if (!j.Aligned)
            {
                list.Add(j);
            }
        }

        return list;
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
        out List<string> path,
        out float totalCost)
    {
        path = new List<string>();
        totalCost = 0f;
        var costSoFar = new Dictionary<string, float>(StringComparer.Ordinal) { [origin] = 0f };
        var cameFrom = new Dictionary<string, string>(StringComparer.Ordinal) { [origin] = origin };
        var open = new List<string> { origin };

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

            foreach (var hop in hops)
            {
                var next = hop.ToTrackId;
                var step = hop.Cost;
                if (hop.RequiresReverse)
                {
                    step += PathTrackCosts.ReversePenalty;
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
