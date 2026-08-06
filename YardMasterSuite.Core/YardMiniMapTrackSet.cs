using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Usable track keys for one yard mini-map schematic (4.13 follow-on).
/// Named catalog rails plus anonymous <c>#Y</c> connectors so paths (e.g. D yard → TT) show.
/// </summary>
public static class YardMiniMapTrackSet
{
    /// <summary>Max anonymous hops from a yard seed before stopping (caps inter-city #Y mesh).</summary>
    public const int DefaultMaxAnonymousHops = 8;

    /// <summary>
    /// Collect track keys to draw for <paramref name="yardId"/>.
    /// Seeds: named (or yard-tagged) keys in <paramref name="seedTrackKeys"/>.
    /// Grow through anonymous neighbors via <paramref name="edges"/>; do not cross foreign named yards.
    /// </summary>
    /// <param name="yardOfKey">
    /// Yard for a key when known (named display id, <c>YardIdOf(rail)</c>, or alias map).
    /// Null/empty = pure anonymous connector.
    /// </param>
    public static HashSet<string> CollectUsableTrackKeys(
        string? yardId,
        IEnumerable<string?>? seedTrackKeys,
        IReadOnlyList<PathEdge>? edges,
        Func<string, string?>? yardOfKey,
        int maxAnonymousHops = DefaultMaxAnonymousHops)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard))
        {
            return result;
        }

        yardOfKey ??= _ => null;
        var maxHops = maxAnonymousHops < 0 ? 0 : maxAnonymousHops;

        // Queue: (trackKey, anonymousHopsFromYardSeed)
        var queue = new Queue<(string Key, int Hops)>();

        if (seedTrackKeys != null)
        {
            foreach (var raw in seedTrackKeys)
            {
                var key = Normalize(raw);
                if (key == null || result.Contains(key))
                {
                    continue;
                }

                var keyYard = NormalizeYard(yardOfKey(key));
                // Only yard-tagged seeds expand. Pure anonymous keys are reached via edges.
                if (keyYard == null
                    || !string.Equals(keyYard, yard, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(key);
                queue.Enqueue((key, 0));
            }
        }

        if (result.Count == 0 || edges == null || edges.Count == 0)
        {
            return result;
        }

        var adj = BuildAdjacency(edges);
        var visitedHops = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var key in result)
        {
            visitedHops[key] = 0;
        }

        while (queue.Count > 0)
        {
            var (from, hops) = queue.Dequeue();
            if (!adj.TryGetValue(from, out var neighbors))
            {
                continue;
            }

            for (var i = 0; i < neighbors.Count; i++)
            {
                var to = neighbors[i];
                var toYard = NormalizeYard(yardOfKey(to));

                if (toYard != null
                    && !string.Equals(toYard, yard, StringComparison.OrdinalIgnoreCase))
                {
                    // Foreign named / tagged rail — do not include or traverse.
                    continue;
                }

                int nextHops;
                if (toYard != null
                    && string.Equals(toYard, yard, StringComparison.OrdinalIgnoreCase))
                {
                    nextHops = 0;
                }
                else
                {
                    // Pure anonymous (#Y) hop.
                    nextHops = hops + 1;
                    if (nextHops > maxHops)
                    {
                        continue;
                    }
                }

                if (visitedHops.TryGetValue(to, out var prev) && prev <= nextHops)
                {
                    continue;
                }

                visitedHops[to] = nextHops;
                result.Add(to);
                queue.Enqueue((to, nextHops));
            }
        }

        return result;
    }

    private static Dictionary<string, List<string>> BuildAdjacency(IReadOnlyList<PathEdge> edges)
    {
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        for (var i = 0; i < edges.Count; i++)
        {
            var from = Normalize(edges[i].FromTrackId);
            var to = Normalize(edges[i].ToTrackId);
            if (from == null || to == null || string.Equals(from, to, StringComparison.Ordinal))
            {
                continue;
            }

            AddAdj(adj, from, to);
            AddAdj(adj, to, from);
        }

        return adj;
    }

    private static void AddAdj(Dictionary<string, List<string>> adj, string from, string to)
    {
        if (!adj.TryGetValue(from, out var list))
        {
            list = new List<string>(4);
            adj[from] = list;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], to, StringComparison.Ordinal))
            {
                return;
            }
        }

        list.Add(to);
    }

    private static string? Normalize(string? trackId)
    {
        var id = trackId?.Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }

    private static string? NormalizeYard(string? yardId)
    {
        var y = yardId?.Trim();
        return string.IsNullOrEmpty(y) ? null : y;
    }
}
