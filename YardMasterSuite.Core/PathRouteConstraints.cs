using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Align Route #4 occupancy + transit filter. Occupied non-dest tracks are hard-blocked.
/// Named (or aliased) non-destination yards permit empty Through rails only.
/// Anonymous <c>#Y-…</c> connectors that only feed occupied named rails are expanded into the
/// occupied set so Dijkstra cannot sneak around cars on HB-* via junction backbone ids.
/// </summary>
public static class PathRouteConstraints
{
    /// <summary>Build occupied-track set from car track keys (one-pass inverted index).</summary>
    public static HashSet<string> OccupiedSet(IEnumerable<string?>? trackKeys)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (trackKeys == null)
        {
            return set;
        }

        foreach (var key in trackKeys)
        {
            var id = Normalize(key);
            if (id != null)
            {
                set.Add(id);
            }
        }

        return set;
    }

    /// <summary>
    /// True for DV anonymous junction/backbone keys (<c>#Y-…</c>), not named city tracks.
    /// </summary>
    public static bool IsAnonymousTrack(string? trackId)
    {
        var id = Normalize(trackId);
        return id != null && id.StartsWith("#", StringComparison.Ordinal);
    }

    /// <summary>
    /// One-hop only: paint anonymous <c>#Y</c> stubs that directly touch an occupied
    /// <b>named</b> rail. No BFS through the #Y mesh — free unnamed pass-through lanes
    /// (also <c>#Y</c>, no Track chip) must stay usable.
    /// <para>
    /// Do not expand from <paramref name="excludeExpandFrom"/> (typically origin + dest):
    /// cars already on the pickup/delivery track must not paint the approach stubs,
    /// or Align/Prep cannot reach an occupied dest (Switch List / couple).
    /// </para>
    /// </summary>
    public static HashSet<string> ExpandOccupiedThroughAnonymous(
        ISet<string>? occupied,
        IReadOnlyList<PathEdge>? edges,
        string? excludeExpandFrom = null,
        string? excludeExpandFrom2 = null)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (occupied != null)
        {
            foreach (var id in occupied)
            {
                var n = Normalize(id);
                if (n != null)
                {
                    set.Add(n);
                }
            }
        }

        if (edges == null || edges.Count == 0 || set.Count == 0)
        {
            return set;
        }

        var exclude = new HashSet<string>(StringComparer.Ordinal);
        var ex1 = Normalize(excludeExpandFrom);
        var ex2 = Normalize(excludeExpandFrom2);
        if (ex1 != null)
        {
            exclude.Add(ex1);
        }

        if (ex2 != null)
        {
            exclude.Add(ex2);
        }

        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Link(string a, string b)
        {
            if (!adj.TryGetValue(a, out var list))
            {
                list = new List<string>();
                adj[a] = list;
            }

            list.Add(b);
        }

        foreach (var e in edges)
        {
            var from = Normalize(e.FromTrackId);
            var to = Normalize(e.ToTrackId);
            if (from == null || to == null || string.Equals(from, to, StringComparison.Ordinal))
            {
                continue;
            }

            Link(from, to);
            Link(to, from);
        }

        // Snapshot named occupied seeds — do not grow from newly painted #Y.
        // Skip exclude tracks so dest/origin cars don't seal their own approaches.
        var namedSeeds = new List<string>();
        foreach (var id in set)
        {
            if (!IsAnonymousTrack(id) && !exclude.Contains(id))
            {
                namedSeeds.Add(id);
            }
        }

        foreach (var named in namedSeeds)
        {
            if (!adj.TryGetValue(named, out var neighbors))
            {
                continue;
            }

            foreach (var n in neighbors)
            {
                if (IsAnonymousTrack(n))
                {
                    set.Add(n);
                }
            }
        }

        return set;
    }

    /// <summary>Yard code from display id <c>HB-E5O</c> → <c>HB</c>; null for <c>#Y-…</c>.</summary>
    public static string? YardIdOf(string? trackId)
    {
        var id = Normalize(trackId);
        if (id == null || id.StartsWith("#", StringComparison.Ordinal))
        {
            return null;
        }

        if (!LocoRadarDisplay.TrackIncludesCity(id))
        {
            return null;
        }

        var dash = id.IndexOf('-');
        var yard = id.Substring(0, dash).Trim();
        return LocoRadarDisplay.IsUsableCityYardId(yard) ? yard : null;
    }

    /// <summary>
    /// Dest yard for FilterEdges: track meta first; else session/sticky yard when dest is anonymous
    /// (Town TT Align — <c>#Y</c> bridge has no city prefix, but delivery is still in-town).
    /// </summary>
    public static string? EffectiveDestYardId(
        string? destTrackId,
        string? sessionYardId,
        Func<string, string?>? yardFor = null)
    {
        yardFor ??= YardIdOf;
        var dest = Normalize(destTrackId);
        if (dest != null)
        {
            var fromMeta = yardFor(dest);
            if (!string.IsNullOrWhiteSpace(fromMeta))
            {
                return fromMeta!.Trim();
            }
        }

        var session = sessionYardId?.Trim();
        return LocoRadarDisplay.IsUsableCityYardId(session) ? session : null;
    }

    /// <summary>
    /// True when entering <paramref name="toTrackId"/> is forbidden for this plan.
    /// <para><b>PRODUCT LOCK (3.5 #4) — do not weaken without an explicit player decision:</b></para>
    /// <list type="number">
    /// <item>Dest track never blocked (delivery).</item>
    /// <item>Occupied non-dest = hard-block.</item>
    /// <item>Origin + intermediate named yards: <see cref="PathTrackClass.Through"/> only
    /// (empty I→O / main / passenger). YardService / Spur / Unknown are not transit.</item>
    /// <item>No free Through through a city ⇒ Dijkstra skips that city (other corridor / NoPath).</item>
    /// </list>
    /// Use <paramref name="yardFor"/> so <c>#Y</c> aliases of named rails still get the yard rule.
    /// Pass <paramref name="destYardOverride"/> when dest is anonymous but known in-town (session yard).
    /// </summary>
    public static bool IsEntryBlocked(
        string? toTrackId,
        PathTrackClass trackClass,
        ISet<string>? occupied,
        string? originTrackId,
        string? destTrackId,
        Func<string, string?>? yardFor = null,
        string? destYardOverride = null)
    {
        var to = Normalize(toTrackId);
        if (to == null)
        {
            return true;
        }

        var dest = Normalize(destTrackId);
        if (dest != null && string.Equals(to, dest, StringComparison.Ordinal))
        {
            return false;
        }

        var origin = Normalize(originTrackId);
        if (origin != null && string.Equals(to, origin, StringComparison.Ordinal))
        {
            return false;
        }

        if (occupied != null && occupied.Contains(to))
        {
            return true;
        }

        yardFor ??= YardIdOf;
        var yard = yardFor(to);
        if (yard == null)
        {
            return false; // anonymous backbone with no named-yard alias
        }

        var destYard = !string.IsNullOrWhiteSpace(destYardOverride)
            ? destYardOverride!.Trim()
            : (dest == null ? null : yardFor(dest));
        if (destYard != null
            && string.Equals(yard, destYard, StringComparison.OrdinalIgnoreCase))
        {
            return false; // delivery yard may use service/storage rails
        }

        // PRODUCT LOCK: origin + intermediate cities — Through only (see summary).
        return trackClass != PathTrackClass.Through;
    }

    /// <summary>Drop edges that enter a blocked track (keeps graph for Dijkstra).</summary>
    public static List<PathEdge> FilterEdges(
        IReadOnlyList<PathEdge> edges,
        Func<string, PathTrackClass> classFor,
        ISet<string>? occupied,
        string? originTrackId,
        string? destTrackId,
        Func<string, string?>? yardFor = null,
        string? destYardOverride = null)
    {
        var list = new List<PathEdge>();
        if (edges == null)
        {
            return list;
        }

        classFor ??= (_ => PathTrackClass.Unknown);
        foreach (var e in edges)
        {
            var to = Normalize(e.ToTrackId);
            if (to == null)
            {
                continue;
            }

            var cls = classFor(to);
            if (IsEntryBlocked(
                    to, cls, occupied, originTrackId, destTrackId, yardFor, destYardOverride))
            {
                continue;
            }

            list.Add(e);
        }

        return list;
    }

    private static string? Normalize(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
