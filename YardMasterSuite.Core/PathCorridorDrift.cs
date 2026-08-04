using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Corridor membership for Align Path OK / stale (3.5). Dijkstra hops can skip short
/// connector rails the bogie still reports — treat A→X→B fill-ins as on-route.
/// </summary>
public static class PathCorridorDrift
{
    /// <summary>
    /// Insert unique intermediates X where the graph has A—X—B between consecutive plan hops.
    /// </summary>
    public static IReadOnlyList<string> ExpandFillIns(
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathEdge> edges)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (trackIds.Count == 1 || edges == null || edges.Count == 0)
        {
            return trackIds;
        }

        var undirected = BuildUndirected(edges);
        var expanded = new List<string>(trackIds.Count + 8);
        expanded.Add(trackIds[0]);

        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var a = Normalize(trackIds[i]);
            var b = Normalize(trackIds[i + 1]);
            if (a == null || b == null)
            {
                if (b != null)
                {
                    expanded.Add(b);
                }

                continue;
            }

            foreach (var x in FillInsBetween(undirected, a, b))
            {
                if (!expanded.Exists(t => string.Equals(t, x, StringComparison.Ordinal)))
                {
                    expanded.Add(x);
                }
            }

            if (!expanded.Exists(t => string.Equals(t, b, StringComparison.Ordinal)))
            {
                expanded.Add(b);
            }
        }

        return expanded;
    }

    /// <summary>
    /// True when <paramref name="currentTrackId"/> is a plan hop or a one-hop fill-in
    /// between two consecutive plan tracks.
    /// </summary>
    public static bool IsOnRoute(
        IReadOnlyList<string> trackIds,
        string? currentTrackId,
        IReadOnlyList<PathEdge> edges)
    {
        var current = Normalize(currentTrackId);
        if (current == null || trackIds == null || trackIds.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < trackIds.Count; i++)
        {
            if (string.Equals(Normalize(trackIds[i]), current, StringComparison.Ordinal))
            {
                return true;
            }
        }

        if (edges == null || edges.Count == 0 || trackIds.Count < 2)
        {
            return false;
        }

        var undirected = BuildUndirected(edges);
        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var a = Normalize(trackIds[i]);
            var b = Normalize(trackIds[i + 1]);
            if (a == null || b == null)
            {
                continue;
            }

            if (IsFillIn(undirected, a, b, current))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> FillInsBetween(
        Dictionary<string, HashSet<string>> undirected,
        string a,
        string b)
    {
        if (!undirected.TryGetValue(a, out var fromA))
        {
            yield break;
        }

        foreach (var x in fromA)
        {
            if (string.Equals(x, b, StringComparison.Ordinal)
                || string.Equals(x, a, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsFillIn(undirected, a, b, x))
            {
                yield return x;
            }
        }
    }

    /// <summary>X is between A and B when A—X and X—B (and X is neither end).</summary>
    private static bool IsFillIn(
        Dictionary<string, HashSet<string>> undirected,
        string a,
        string b,
        string x)
    {
        if (string.Equals(x, a, StringComparison.Ordinal)
            || string.Equals(x, b, StringComparison.Ordinal))
        {
            return false;
        }

        return HasUndirected(undirected, a, x) && HasUndirected(undirected, x, b);
    }

    private static bool HasUndirected(
        Dictionary<string, HashSet<string>> undirected,
        string from,
        string to)
    {
        return undirected.TryGetValue(from, out var set) && set.Contains(to);
    }

    private static Dictionary<string, HashSet<string>> BuildUndirected(IReadOnlyList<PathEdge> edges)
    {
        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            var from = Normalize(e.FromTrackId);
            var to = Normalize(e.ToTrackId);
            if (from == null || to == null || string.Equals(from, to, StringComparison.Ordinal))
            {
                continue;
            }

            Link(map, from, to);
            Link(map, to, from);
        }

        return map;
    }

    private static void Link(Dictionary<string, HashSet<string>> map, string from, string to)
    {
        if (!map.TryGetValue(from, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            map[from] = set;
        }

        set.Add(to);
    }

    /// <summary>
    /// Snapshot of plan-junction <b>required</b> branches at Set dest / Align.
    /// Freeze the corridor intent — not a live read that can flap before Switch settles.
    /// Empty when the corridor has no switches.
    /// </summary>
    public static Dictionary<string, int> CaptureJunctionBranches(
        PathPlanResult plan,
        IReadOnlyDictionary<string, int> liveSelectedBranches)
    {
        var snap = new Dictionary<string, int>(StringComparer.Ordinal);
        if (plan?.Junctions == null || plan.Junctions.Count == 0)
        {
            return snap;
        }

        // liveSelectedBranches kept for call-site compatibility / future diagnostics.
        _ = liveSelectedBranches;

        foreach (var j in plan.Junctions)
        {
            if (string.IsNullOrEmpty(j.JunctionId) || snap.ContainsKey(j.JunctionId))
            {
                continue;
            }

            // RequiredBranch is the Dijkstra hop intent; ActualBranch can be a defaulted 0
            // when the live map briefly lacks the key.
            snap[j.JunctionId] = j.RequiredBranch;
        }

        return snap;
    }

    /// <summary>
    /// Watch corridor switch throws only while Path OK. Misaligned plans already show
    /// <c>Path N wrong</c> — do not MarkStale while Align is still throwing.
    /// </summary>
    public static bool ShouldWatchJunctionDrift(PathCheckStatus status) =>
        status == PathCheckStatus.Aligned;

    /// <summary>
    /// True when every <b>observable</b> snapshotted junction still matches.
    /// Missing live keys are <b>not</b> throws (cold/partial map) — fail open.
    /// Null / empty live ⇒ unchanged (cannot observe).
    /// </summary>
    public static bool JunctionsUnchanged(
        IReadOnlyDictionary<string, int>? snapshot,
        IReadOnlyDictionary<string, int>? liveSelectedBranches)
    {
        if (snapshot == null || snapshot.Count == 0)
        {
            return true;
        }

        if (liveSelectedBranches == null || liveSelectedBranches.Count == 0)
        {
            return true;
        }

        foreach (var kv in snapshot)
        {
            if (!liveSelectedBranches.TryGetValue(kv.Key, out var live))
            {
                continue;
            }

            if (live != kv.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when a junction used by the frozen plan no longer has its frozen branch.
    /// Unrelated world switches and unreadable keys do not invalidate the route.
    /// </summary>
    public static bool PlannedJunctionChanged(
        IReadOnlyDictionary<string, int>? snapshot,
        IReadOnlyDictionary<string, int>? liveSelectedBranches)
    {
        return snapshot != null
            && snapshot.Count > 0
            && !JunctionsUnchanged(snapshot, liveSelectedBranches);
    }

    /// <summary>
    /// Human drift list for Player.log: <c>Jid freeze→live</c> (missing live as <c>?</c>).
    /// </summary>
    public static string FormatJunctionDrift(
        IReadOnlyDictionary<string, int>? snapshot,
        IReadOnlyDictionary<string, int>? liveSelectedBranches,
        int head = 12)
    {
        if (snapshot == null || snapshot.Count == 0)
        {
            return "—";
        }

        var parts = new List<string>();
        foreach (var kv in snapshot)
        {
            string? piece = null;
            if (liveSelectedBranches == null
                || !liveSelectedBranches.TryGetValue(kv.Key, out var live))
            {
                piece = kv.Key + " " + kv.Value + "→?";
            }
            else if (live != kv.Value)
            {
                piece = kv.Key + " " + kv.Value + "→" + live;
            }

            if (piece == null)
            {
                continue;
            }

            if (parts.Count < head)
            {
                parts.Add(piece);
            }
        }

        if (parts.Count == 0)
        {
            return "—";
        }

        var totalDiffs = 0;
        foreach (var kv in snapshot)
        {
            if (liveSelectedBranches == null
                || !liveSelectedBranches.TryGetValue(kv.Key, out var live)
                || live != kv.Value)
            {
                totalDiffs++;
            }
        }

        var joined = string.Join(" ", parts);
        if (totalDiffs > parts.Count)
        {
            joined += " …+" + (totalDiffs - parts.Count);
        }

        return joined;
    }

    private static string? Normalize(string? trackId)
    {
        var id = trackId?.Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }
}
