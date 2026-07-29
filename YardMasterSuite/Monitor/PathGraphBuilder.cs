using System.Collections.Generic;
using UnityEngine;
using DV.Logic.Job;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Builds path graph + junction maps + destination catalog from live RailTrack / Junction
/// for Align Route (3.5). Read-only — never throws switches.
/// Topology is cached (~120s); selectedBranch is refreshed cheaply on each use.
/// </summary>
internal static class PathGraphBuilder
{
    private const float TopologyCacheSeconds = 120f;

    private static float _builtAt = -999f;
    private static List<PathEdge>? _edges;
    private static Dictionary<string, int>? _selected;
    private static Dictionary<string, Junction>? _junctionsById;
    private static List<(string YardId, string TrackId)>? _catalog;
    private static Dictionary<string, RailTrack>? _tracksByKey;

    /// <summary>Last rebuild counters for desk diagnostics (e.g. Reload list).</summary>
    public static string LastDiag { get; private set; } = "";

    public static bool TryBuild(
        out List<PathEdge> edges,
        out Dictionary<string, int> junctionSelectedBranch) =>
        TryBuild(out edges, out junctionSelectedBranch, out _, out _);

    public static bool TryBuild(
        out List<PathEdge> edges,
        out Dictionary<string, int> junctionSelectedBranch,
        out Dictionary<string, Junction> junctionsById,
        out List<(string YardId, string TrackId)> catalog)
    {
        if (_edges != null
            && _selected != null
            && _junctionsById != null
            && _catalog != null
            && _catalog.Count > 0
            && Time.unscaledTime - _builtAt < TopologyCacheSeconds)
        {
            RefreshSelectedBranches(_junctionsById, _selected);
            edges = _edges;
            junctionSelectedBranch = _selected;
            junctionsById = _junctionsById;
            catalog = _catalog;
            return true;
        }

        return Rebuild(out edges, out junctionSelectedBranch, out junctionsById, out catalog);
    }

    /// <summary>Force rebuild (e.g. after Align throws or empty cache).</summary>
    public static void InvalidateCache()
    {
        _builtAt = -999f;
        _edges = null;
        _selected = null;
        _junctionsById = null;
        _catalog = null;
        _tracksByKey = null;
    }

    private static bool Rebuild(
        out List<PathEdge> edges,
        out Dictionary<string, int> junctionSelectedBranch,
        out Dictionary<string, Junction> junctionsById,
        out List<(string YardId, string TrackId)> catalog)
    {
        edges = new List<PathEdge>();
        junctionSelectedBranch = new Dictionary<string, int>();
        junctionsById = new Dictionary<string, Junction>();
        catalog = new List<(string, string)>();

        var tracks = ResolveRailTracks();
        var trackCount = tracks?.Length ?? 0;
        if (tracks == null || trackCount == 0)
        {
            LastDiag = "reg=0 (no RailTracks)";
            // Station catalog alone may still fill the desk.
            AppendStationCatalog(catalog);
            if (catalog.Count == 0)
            {
                return false;
            }

            _edges = edges;
            _selected = junctionSelectedBranch;
            _junctionsById = junctionsById;
            _catalog = catalog;
            _tracksByKey = new Dictionary<string, RailTrack>(System.StringComparer.Ordinal);
            _builtAt = Time.unscaledTime;
            LastDiag = $"reg=0 cat={catalog.Count} (stations only)";
            return true;
        }

        var enterCost = new Dictionary<string, float>();
        var tracksByKey = new Dictionary<string, RailTrack>(System.StringComparer.Ordinal);
        var seenPlain = new HashSet<string>();
        var seenJunctionHop = new HashSet<string>();
        var keyed = 0;

        foreach (var track in tracks)
        {
            if (track == null)
            {
                continue;
            }

            var key = TrackKey(track);
            if (key == null)
            {
                continue;
            }

            keyed++;
            tracksByKey[key] = track;
            enterCost[key] = EnterCostFor(track);
            TryAddCatalogEntry(catalog, track, key);
        }

        AppendStationCatalog(catalog);

        var junctions = RailTrackRegistry.Junctions;
        if (junctions != null)
        {
            foreach (var junction in junctions)
            {
                if (junction == null || junction.outBranches == null)
                {
                    continue;
                }

                var stemTrack = junction.inBranch.track;
                if (stemTrack == null)
                {
                    continue;
                }

                var jid = JunctionKey(junction);
                junctionsById[jid] = junction;
                junctionSelectedBranch[jid] = junction.selectedBranch;

                var stemId = TrackKey(stemTrack);
                if (stemId == null)
                {
                    continue;
                }

                for (var i = 0; i < junction.outBranches.Count; i++)
                {
                    var outId = TrackKey(junction.outBranches[i].track);
                    if (outId == null)
                    {
                        continue;
                    }

                    AddJunctionHop(edges, seenJunctionHop, enterCost, stemId, outId, jid, i);
                    AddJunctionHop(edges, seenJunctionHop, enterCost, outId, stemId, jid, i);
                }
            }
        }

        foreach (var track in tracks)
        {
            if (track == null)
            {
                continue;
            }

            var fromId = TrackKey(track);
            if (fromId == null)
            {
                continue;
            }

            if (track.outJunction == null && track.outIsConnected)
            {
                var toId = TrackKey(track.outBranch.track);
                AddPlainPair(edges, seenPlain, enterCost, fromId, toId);
            }

            if (track.inJunction == null && track.inIsConnected)
            {
                var toId = TrackKey(track.inBranch.track);
                AddPlainPair(edges, seenPlain, enterCost, fromId, toId);
            }
        }

        MarkStubApproachesAsReverse(edges);

        LastDiag = $"reg={trackCount} keys={keyed} cat={catalog.Count} edges={edges.Count}";

        // Desk needs catalog; pathfinding needs edges. Allow catalog-only success.
        if (catalog.Count == 0)
        {
            return false;
        }

        _edges = edges;
        _selected = junctionSelectedBranch;
        _junctionsById = junctionsById;
        _catalog = catalog;
        _tracksByKey = tracksByKey;
        _builtAt = Time.unscaledTime;
        return true;
    }

    private static RailTrack[]? ResolveRailTracks()
    {
        try
        {
            var tracks = RailTrackRegistry.RailTracks;
            if (tracks != null && tracks.Length > 0)
            {
                return tracks;
            }

            var instance = RailTrackRegistry.Instance;
            if (instance == null)
            {
                return tracks;
            }

            if (instance.AllTracks != null && instance.AllTracks.Length > 0)
            {
                return instance.AllTracks;
            }

            if (instance.OrderedRailtracks != null && instance.OrderedRailtracks.Length > 0)
            {
                return instance.OrderedRailtracks;
            }

            return tracks;
        }
        catch
        {
            return null;
        }
    }

    private static void TryAddCatalogEntry(
        List<(string YardId, string TrackId)> catalog,
        RailTrack track,
        string key)
    {
        var yard = YardIdOf(track) ?? YardIdFromTrackKey(key);
        if (!LocoRadarDisplay.IsUsableCityYardId(yard))
        {
            return;
        }

        // Prefer player-facing display ids in the dropdown (SM-A2P), not internal # tokens.
        var display = PreferDisplayKey(track) ?? key;
        if (string.IsNullOrEmpty(display) || display!.StartsWith("#", System.StringComparison.Ordinal))
        {
            return;
        }

        catalog.Add((yard!, display));
    }

    private static void AppendStationCatalog(List<(string YardId, string TrackId)> catalog)
    {
        try
        {
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return;
            }

            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var (y, t) in catalog)
            {
                seen.Add(y + "\0" + t);
            }

            foreach (var station in stations)
            {
                if (station == null)
                {
                    continue;
                }

                var yard = station.stationInfo?.YardID?.Trim();
                if (!LocoRadarDisplay.IsUsableCityYardId(yard))
                {
                    continue;
                }

                var list = station.AllStationTracks;
                if (list == null)
                {
                    continue;
                }

                foreach (var rail in list)
                {
                    if (rail == null)
                    {
                        continue;
                    }

                    var display = PreferDisplayKey(rail) ?? TrackKey(rail);
                    if (string.IsNullOrEmpty(display)
                        || display!.StartsWith("#", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var token = yard + "\0" + display;
                    if (!seen.Add(token))
                    {
                        continue;
                    }

                    catalog.Add((yard!, display));
                }
            }
        }
        catch
        {
            // Station list unavailable — keep registry catalog only.
        }
    }

    private static void RefreshSelectedBranches(
        Dictionary<string, Junction> junctionsById,
        Dictionary<string, int> selected)
    {
        foreach (var kv in junctionsById)
        {
            if (kv.Value != null)
            {
                selected[kv.Key] = kv.Value.selectedBranch;
            }
        }
    }

    /// <summary>
    /// Graph node id — keep internal <c>#…</c> keys so junctions/mainline connect.
    /// </summary>
    public static string? TrackKey(RailTrack? rail)
    {
        if (rail == null)
        {
            return null;
        }

        try
        {
            return TrackKey(LogicTrackOf(rail));
        }
        catch
        {
            return PreferDisplayKey(rail);
        }
    }

    public static string? TrackKey(Track? logic)
    {
        if (logic?.ID == null)
        {
            return null;
        }

        var id = logic.ID;
        var display = id.FullDisplayID?.Trim();
        if (!string.IsNullOrEmpty(display))
        {
            return display;
        }

        var full = id.FullID?.Trim();
        return string.IsNullOrEmpty(full) ? null : full;
    }

    private static string? PreferDisplayKey(RailTrack? rail)
    {
        try
        {
            var id = LogicTrackOf(rail)?.ID;
            var display = id?.FullDisplayID?.Trim();
            if (!string.IsNullOrEmpty(display))
            {
                return display;
            }

            return id?.FullID?.Trim();
        }
        catch
        {
            return null;
        }
    }

    private static Track? LogicTrackOf(RailTrack? rail)
    {
        if (rail == null)
        {
            return null;
        }

        try
        {
            var map = RailTrackRegistry.RailTrackToLogicTrack;
            if (map != null && map.TryGetValue(rail, out var logic) && logic != null)
            {
                return logic;
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            return rail.LogicTrack();
        }
        catch
        {
            return null;
        }
    }

    public static string? YardIdOf(RailTrack? rail)
    {
        try
        {
            var yard = LogicTrackOf(rail)?.ID?.yardId?.Trim();
            return LocoRadarDisplay.IsUsableCityYardId(yard) ? yard : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Derive yard from display id <c>SM-A2P</c> → <c>SM</c>.</summary>
    public static string? YardIdFromTrackKey(string? trackKey)
    {
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key) || key!.StartsWith("#", System.StringComparison.Ordinal))
        {
            return null;
        }

        if (!LocoRadarDisplay.TrackIncludesCity(key))
        {
            return null;
        }

        var dash = key.IndexOf('-');
        var yard = key.Substring(0, dash).Trim();
        return LocoRadarDisplay.IsUsableCityYardId(yard) ? yard : null;
    }

    public static bool TryGetTrackWorldXZ(string? trackKey, out float x, out float z)
    {
        x = z = 0f;
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (_tracksByKey == null || !_tracksByKey.TryGetValue(key!, out var rail) || rail == null)
        {
            if (!TryBuild(out _, out _, out _, out _))
            {
                return false;
            }

            if (_tracksByKey == null || !_tracksByKey.TryGetValue(key!, out rail) || rail == null)
            {
                return false;
            }
        }

        try
        {
            var p = rail.transform.position;
            x = p.x;
            z = p.z;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static float EnterCostFor(RailTrack rail)
    {
        try
        {
            var id = LogicTrackOf(rail)?.ID;
            if (id == null)
            {
                return PathTrackCosts.Unknown;
            }

            var typeToken = TryReadTrackType(id) ?? id.FullID ?? id.FullDisplayID;
            return PathTrackCosts.EnterCost(PathTrackCosts.Classify(typeToken));
        }
        catch
        {
            return PathTrackCosts.Unknown;
        }
    }

    private static string? TryReadTrackType(TrackID id)
    {
        try
        {
            var field = typeof(TrackID).GetField(
                "trackType",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(id) as string;
        }
        catch
        {
            return null;
        }
    }

    private static string JunctionKey(Junction junction)
    {
        var data = junction.junctionData;
        if (!string.IsNullOrWhiteSpace(data.junctionIdLong))
        {
            return data.junctionIdLong.Trim();
        }

        if (data.junctionId != 0)
        {
            return data.junctionId.ToString();
        }

        return "J" + junction.GetInstanceID();
    }

    private static void AddJunctionHop(
        List<PathEdge> edges,
        HashSet<string> seen,
        Dictionary<string, float> enterCost,
        string from,
        string to,
        string junctionId,
        int branch)
    {
        var key = from + ">" + to + "|" + junctionId + ":" + branch;
        if (!seen.Add(key))
        {
            return;
        }

        enterCost.TryGetValue(to, out var cost);
        if (cost <= 0f)
        {
            cost = PathTrackCosts.Unknown;
        }

        edges.Add(new PathEdge(from, to, junctionId, branch, cost));
    }

    private static void AddPlainPair(
        List<PathEdge> edges,
        HashSet<string> seen,
        Dictionary<string, float> enterCost,
        string? from,
        string? to)
    {
        if (from == null || to == null || from == to)
        {
            return;
        }

        AddPlain(edges, seen, enterCost, from, to);
        AddPlain(edges, seen, enterCost, to, from);
    }

    private static void AddPlain(
        List<PathEdge> edges,
        HashSet<string> seen,
        Dictionary<string, float> enterCost,
        string from,
        string to)
    {
        var key = from + ">" + to;
        if (!seen.Add(key))
        {
            return;
        }

        enterCost.TryGetValue(to, out var cost);
        if (cost <= 0f)
        {
            cost = PathTrackCosts.Unknown;
        }

        edges.Add(new PathEdge(from, to, cost: cost));
    }

    private static void MarkStubApproachesAsReverse(List<PathEdge> edges)
    {
        var degree = new Dictionary<string, HashSet<string>>();
        foreach (var e in edges)
        {
            if (!degree.TryGetValue(e.FromTrackId, out var fromSet))
            {
                fromSet = new HashSet<string>();
                degree[e.FromTrackId] = fromSet;
            }

            fromSet.Add(e.ToTrackId);

            if (!degree.TryGetValue(e.ToTrackId, out var toSet))
            {
                toSet = new HashSet<string>();
                degree[e.ToTrackId] = toSet;
            }

            toSet.Add(e.FromTrackId);
        }

        for (var i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (!degree.TryGetValue(e.ToTrackId, out var neighbors) || neighbors.Count != 1)
            {
                continue;
            }

            edges[i] = new PathEdge(
                e.FromTrackId,
                e.ToTrackId,
                e.JunctionId,
                e.RequiredBranch,
                e.Cost,
                requiresReverse: true);
        }
    }
}
