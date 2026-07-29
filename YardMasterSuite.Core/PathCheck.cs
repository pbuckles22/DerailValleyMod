using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Directed topology hop for path check / Align Route. Junction hops carry a required branch.</summary>
public readonly struct PathEdge
{
    public PathEdge(
        string fromTrackId,
        string toTrackId,
        string? junctionId = null,
        int requiredBranch = -1,
        float cost = 1f,
        bool requiresReverse = false)
    {
        FromTrackId = fromTrackId ?? string.Empty;
        ToTrackId = toTrackId ?? string.Empty;
        JunctionId = string.IsNullOrWhiteSpace(junctionId) ? null : junctionId!.Trim();
        RequiredBranch = requiredBranch;
        Cost = cost > 0f ? cost : 1f;
        RequiresReverse = requiresReverse;
    }

    public string FromTrackId { get; }
    public string ToTrackId { get; }
    public string? JunctionId { get; }
    public int RequiredBranch { get; }
    public float Cost { get; }
    public bool RequiresReverse { get; }

    public bool HasJunction => JunctionId != null && RequiredBranch >= 0;
}

/// <summary>One junction along a found path vs live selected branch.</summary>
public readonly struct PathJunctionEval
{
    public PathJunctionEval(string junctionId, int requiredBranch, int actualBranch)
    {
        JunctionId = junctionId;
        RequiredBranch = requiredBranch;
        ActualBranch = actualBranch;
    }

    public string JunctionId { get; }
    public int RequiredBranch { get; }
    public int ActualBranch { get; }
    public bool Aligned => RequiredBranch == ActualBranch;
}

public enum PathCheckStatus
{
    NoDestination = 0,
    NoOrigin = 1,
    NoPath = 2,
    Aligned = 3,
    Misaligned = 4,
}

/// <summary>Result of “check my math” for origin → destination (3.4).</summary>
public sealed class PathCheckResult
{
    public PathCheckResult(
        PathCheckStatus status,
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathJunctionEval> junctions,
        int misalignedCount)
    {
        Status = status;
        TrackIds = trackIds;
        Junctions = junctions;
        MisalignedCount = misalignedCount;
    }

    public PathCheckStatus Status { get; }
    public IReadOnlyList<string> TrackIds { get; }
    public IReadOnlyList<PathJunctionEval> Junctions { get; }
    public int MisalignedCount { get; }
}

/// <summary>
/// Pure path tracer for <b>3.4</b>: shortest topology path + junction alignment
/// (no auto-throw). Graph edges encode required branch; live map is selectedBranch.
/// </summary>
public static class PathCheck
{
    public static PathCheckResult Evaluate(
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
            return new PathCheckResult(
                PathCheckStatus.Aligned,
                new[] { origin },
                Array.Empty<PathJunctionEval>(),
                0);
        }

        var adj = BuildAdjacency(edges);
        if (!TryBfs(adj, origin, dest, out var path))
        {
            return Empty(PathCheckStatus.NoPath);
        }

        var junctionEvals = new List<PathJunctionEval>();
        var misaligned = 0;
        var selected = junctionSelectedBranch ?? new Dictionary<string, int>();

        for (var i = 0; i < path.Count - 1; i++)
        {
            var from = path[i];
            var to = path[i + 1];
            if (!TryGetHop(adj, from, to, out var hop) || !hop.HasJunction || hop.JunctionId == null)
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
        return new PathCheckResult(status, path, junctionEvals, misaligned);
    }

    private static PathCheckResult Empty(PathCheckStatus status) =>
        new(status, Array.Empty<string>(), Array.Empty<PathJunctionEval>(), 0);

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

    private static bool TryBfs(
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string dest,
        out List<string> path)
    {
        path = new List<string>();
        var cameFrom = new Dictionary<string, string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(origin);
        cameFrom[origin] = origin;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (string.Equals(current, dest, StringComparison.Ordinal))
            {
                path = Reconstruct(cameFrom, origin, dest);
                return true;
            }

            if (!adj.TryGetValue(current, out var hops))
            {
                continue;
            }

            foreach (var hop in hops)
            {
                var next = hop.ToTrackId;
                if (cameFrom.ContainsKey(next))
                {
                    continue;
                }

                cameFrom[next] = current;
                queue.Enqueue(next);
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
