using System.Collections.Generic;
using UnityEngine;
using DV.Logic.Job;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Builds path graph + junction maps + destination catalog from live RailTrack / Junction
/// for Align Route (3.5). Read-only — never throws switches.
/// Topology/catalog are session-scoped: warm once, keep until Reload or world exit.
/// selectedBranch is refreshed cheaply on each use. Cold build is frame-pumped.
/// </summary>
internal static class PathGraphBuilder
{
    /// <summary>Tracks (meta) + plains per frame — keep under a hitch budget.</summary>
    public const int DefaultMapBudgetPerFrame = 64;

    private enum MapPhase
    {
        None,
        Tracks,
        Junctions,
        Plains,
        Finalize,
    }

    private static float _builtAt = -999f;
    private static List<PathEdge>? _edges;
    private static Dictionary<string, int>? _selected;
    private static Dictionary<string, Junction>? _junctionsById;
    private static List<(string YardId, string TrackId)>? _catalog;
    private static Dictionary<string, RailTrack>? _tracksByKey;
    private static Dictionary<string, PathTrackMeta>? _metaByKey;

    private static readonly PathGraphBuildPump Pump = new();
    private static MapPhase _mapPhase = MapPhase.None;
    private static RailTrack[]? _mapTracks;
    private static Junction[]? _mapJunctions;
    private static int _mapTrackIndex;
    private static int _mapJunctionIndex;
    private static int _mapPlainIndex;
    private static int _mapKeyed;
    private static List<PathEdge>? _mapEdges;
    private static Dictionary<string, int>? _mapSelected;
    private static Dictionary<string, Junction>? _mapJunctionsById;
    private static List<(string YardId, string TrackId)>? _mapCatalog;
    private static Dictionary<string, float>? _mapEnterCost;
    private static Dictionary<string, RailTrack>? _mapTracksByKey;
    private static Dictionary<string, PathTrackMeta>? _mapMetaByKey;
    private static HashSet<string>? _mapSeenPlain;
    private static HashSet<string>? _mapSeenJunctionHop;

    /// <summary>Last rebuild counters for desk diagnostics (e.g. Reload list).</summary>
    public static string LastDiag { get; private set; } = "";

    /// <summary>True while a frame-pumped rebuild is in progress.</summary>
    public static bool IsMapping => Pump.IsMapping;

    /// <summary>Banner text while mapping; empty when idle/ready.</summary>
    public static string MappingBanner =>
        Pump.IsMapping ? PathGraphBuildPump.FormatBanner(Pump.Progress01) : "";

    /// <summary>True when topology/catalog are warm for this world session.</summary>
    public static bool HasReadyCache =>
        _edges != null
        && _selected != null
        && _junctionsById != null
        && _catalog != null
        && _catalog.Count > 0;

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
        if (HasReadyCache)
        {
            RefreshSelectedBranches(_junctionsById!, _selected!);
            edges = _edges!;
            junctionSelectedBranch = _selected!;
            junctionsById = _junctionsById!;
            catalog = _catalog!;
            return true;
        }

        // Never sync-rebuild on the calling thread — start/continue the pump instead.
        EnsureMappingStarted();
        edges = new List<PathEdge>();
        junctionSelectedBranch = new Dictionary<string, int>();
        junctionsById = new Dictionary<string, Junction>();
        catalog = new List<(string, string)>();
        return false;
    }

    /// <summary>Force rebuild (e.g. Reload list). Cancels any in-flight pump.</summary>
    public static void InvalidateCache()
    {
        _builtAt = -999f;
        _edges = null;
        _selected = null;
        _junctionsById = null;
        _catalog = null;
        _tracksByKey = null;
        _metaByKey = null;
        CancelMapping();
    }

    /// <summary>Begin frame-pumped rebuild when cache is cold (no-op if warm or already mapping).</summary>
    public static void EnsureMappingStarted()
    {
        if (HasReadyCache || Pump.IsMapping)
        {
            return;
        }

        BeginMapping();
    }

    /// <summary>
    /// Process up to <paramref name="maxItemsPerFrame"/> tracks/junctions this frame.
    /// Returns true on the frame mapping finishes (ready or failed).
    /// </summary>
    public static bool TickMapping(int maxItemsPerFrame = DefaultMapBudgetPerFrame)
    {
        if (!Pump.IsMapping)
        {
            return false;
        }

        if (maxItemsPerFrame < 1)
        {
            maxItemsPerFrame = 1;
        }

        var budget = maxItemsPerFrame;
        while (budget > 0 && Pump.IsMapping)
        {
            switch (_mapPhase)
            {
                case MapPhase.Tracks:
                    budget = TickTracks(budget);
                    break;
                case MapPhase.Junctions:
                    budget = TickJunctions(budget);
                    break;
                case MapPhase.Plains:
                    budget = TickPlains(budget);
                    break;
                case MapPhase.Finalize:
                    FinishMapping();
                    return true;
                default:
                    CancelMapping();
                    Pump.Fail();
                    LastDiag = "map phase lost";
                    return true;
            }
        }

        return !Pump.IsMapping;
    }

    private static void BeginMapping()
    {
        CancelMapping();

        var tracks = ResolveRailTracks();
        var trackCount = tracks?.Length ?? 0;
        if (tracks == null || trackCount == 0)
        {
            var catalog = new List<(string YardId, string TrackId)>();
            AppendStationCatalog(catalog);
            if (catalog.Count == 0)
            {
                Pump.Begin(1);
                Pump.Fail();
                LastDiag = "reg=0 (no RailTracks)";
                return;
            }

            PublishCache(
                new List<PathEdge>(),
                new Dictionary<string, int>(),
                new Dictionary<string, Junction>(),
                catalog,
                new Dictionary<string, RailTrack>(System.StringComparer.Ordinal),
                new Dictionary<string, PathTrackMeta>(System.StringComparer.Ordinal));
            Pump.Begin(1);
            Pump.Complete();
            LastDiag = $"reg=0 cat={catalog.Count} (stations only)";
            return;
        }

        Junction[]? junctions = null;
        try
        {
            junctions = RailTrackRegistry.Junctions;
        }
        catch
        {
            junctions = null;
        }

        var junctionCount = junctions?.Length ?? 0;
        // Tracks + plains pass + junction pass + finalize unit.
        Pump.Begin(trackCount + trackCount + junctionCount + 1);

        _mapTracks = tracks;
        _mapJunctions = junctions;
        _mapTrackIndex = 0;
        _mapJunctionIndex = 0;
        _mapPlainIndex = 0;
        _mapKeyed = 0;
        _mapEdges = new List<PathEdge>();
        _mapSelected = new Dictionary<string, int>();
        _mapJunctionsById = new Dictionary<string, Junction>();
        _mapCatalog = new List<(string, string)>();
        _mapEnterCost = new Dictionary<string, float>(System.StringComparer.Ordinal);
        _mapTracksByKey = new Dictionary<string, RailTrack>(System.StringComparer.Ordinal);
        _mapMetaByKey = new Dictionary<string, PathTrackMeta>(System.StringComparer.Ordinal);
        _mapSeenPlain = new HashSet<string>();
        _mapSeenJunctionHop = new HashSet<string>();
        _mapPhase = MapPhase.Tracks;
        LastDiag = $"mapping reg={trackCount}…";
    }

    private static int TickTracks(int budget)
    {
        var tracks = _mapTracks!;
        var enterCost = _mapEnterCost!;
        var tracksByKey = _mapTracksByKey!;
        var metaByKey = _mapMetaByKey!;
        var catalog = _mapCatalog!;

        while (budget > 0 && _mapTrackIndex < tracks.Length)
        {
            var track = tracks[_mapTrackIndex++];
            budget--;
            Pump.AddCompleted(1);

            if (track == null)
            {
                continue;
            }

            var key = TrackKey(track);
            if (key == null)
            {
                continue;
            }

            _mapKeyed++;
            RegisterTrackKeys(tracksByKey, track, key);
            var cls = ClassifyTrack(track);
            var len = LengthMetersOf(track);
            var geo = GeometryLimitKmh(track);
            var meta = new PathTrackMeta(len, geo, cls);
            metaByKey[key] = meta;
            var hop = PathTrackCosts.TravelSeconds(len, geo, cls);
            enterCost[key] = hop;
            foreach (var alias in AlternateTrackKeys(track, key))
            {
                enterCost[alias] = hop;
                metaByKey[alias] = meta;
            }

            TryAddCatalogEntry(catalog, track, key);
        }

        if (_mapTrackIndex >= tracks.Length)
        {
            _mapPhase = MapPhase.Junctions;
        }

        return budget;
    }

    private static int TickJunctions(int budget)
    {
        var junctions = _mapJunctions;
        if (junctions == null || junctions.Length == 0)
        {
            _mapPhase = MapPhase.Plains;
            return budget;
        }

        var edges = _mapEdges!;
        var enterCost = _mapEnterCost!;
        var junctionsById = _mapJunctionsById!;
        var selected = _mapSelected!;
        var seenJunctionHop = _mapSeenJunctionHop!;

        while (budget > 0 && _mapJunctionIndex < junctions.Length)
        {
            var junction = junctions[_mapJunctionIndex++];
            budget--;
            Pump.AddCompleted(1);

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
            selected[jid] = junction.selectedBranch;

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

        if (_mapJunctionIndex >= junctions.Length)
        {
            _mapPhase = MapPhase.Plains;
        }

        return budget;
    }

    private static int TickPlains(int budget)
    {
        var tracks = _mapTracks!;
        var edges = _mapEdges!;
        var enterCost = _mapEnterCost!;
        var seenPlain = _mapSeenPlain!;

        while (budget > 0 && _mapPlainIndex < tracks.Length)
        {
            var track = tracks[_mapPlainIndex++];
            budget--;
            Pump.AddCompleted(1);

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

        if (_mapPlainIndex >= tracks.Length)
        {
            _mapPhase = MapPhase.Finalize;
        }

        return budget;
    }

    private static void FinishMapping()
    {
        var edges = _mapEdges!;
        var catalog = _mapCatalog!;
        var trackCount = _mapTracks?.Length ?? 0;

        AppendStationCatalog(catalog);
        MarkStubApproachesAsReverse(edges);

        LastDiag = $"reg={trackCount} keys={_mapKeyed} cat={catalog.Count} edges={edges.Count}";
        Pump.AddCompleted(1);

        if (catalog.Count == 0)
        {
            CancelMapping();
            Pump.Fail();
            LastDiag += " (empty catalog)";
            return;
        }

        PublishCache(
            edges,
            _mapSelected!,
            _mapJunctionsById!,
            catalog,
            _mapTracksByKey!,
            _mapMetaByKey!);
        ClearMapScratch();
        Pump.Complete();
    }

    private static void PublishCache(
        List<PathEdge> edges,
        Dictionary<string, int> selected,
        Dictionary<string, Junction> junctionsById,
        List<(string YardId, string TrackId)> catalog,
        Dictionary<string, RailTrack> tracksByKey,
        Dictionary<string, PathTrackMeta> metaByKey)
    {
        _edges = edges;
        _selected = selected;
        _junctionsById = junctionsById;
        _catalog = catalog;
        _tracksByKey = tracksByKey;
        _metaByKey = metaByKey;
        _builtAt = Time.unscaledTime;
    }

    private static void CancelMapping()
    {
        ClearMapScratch();
        if (Pump.IsMapping || Pump.Current == PathGraphBuildPump.State.Failed
            || Pump.Current == PathGraphBuildPump.State.Ready)
        {
            Pump.Reset();
        }
    }

    private static void ClearMapScratch()
    {
        _mapPhase = MapPhase.None;
        _mapTracks = null;
        _mapJunctions = null;
        _mapTrackIndex = 0;
        _mapJunctionIndex = 0;
        _mapPlainIndex = 0;
        _mapKeyed = 0;
        _mapEdges = null;
        _mapSelected = null;
        _mapJunctionsById = null;
        _mapCatalog = null;
        _mapEnterCost = null;
        _mapTracksByKey = null;
        _mapMetaByKey = null;
        _mapSeenPlain = null;
        _mapSeenJunctionHop = null;
    }

    /// <summary>Planning meta for a graph node (length / geometry / class) — Align debug.</summary>
    public static PathTrackMeta? TryGetTrackMeta(string? trackKey)
    {
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key) || _metaByKey == null)
        {
            return null;
        }

        return _metaByKey.TryGetValue(key!, out var meta) ? meta : null;
    }

    /// <summary>
    /// 0..1 progress along the current corridor track toward the next planned track
    /// (Bezier span). False when unknown — caller should treat as 0.
    /// </summary>
    public static bool TryCorridorHopProgress(
        PathPlanResult plan,
        int corridorIndex,
        out float progress01)
    {
        progress01 = 0f;
        if (plan == null
            || corridorIndex < 0
            || corridorIndex >= plan.TrackIds.Count - 1
            || _tracksByKey == null)
        {
            return false;
        }

        var curKey = plan.TrackIds[corridorIndex]?.Trim();
        var nextKey = plan.TrackIds[corridorIndex + 1]?.Trim();
        if (string.IsNullOrEmpty(curKey) || string.IsNullOrEmpty(nextKey))
        {
            return false;
        }

        if (!_tracksByKey.TryGetValue(curKey!, out var rail) || rail == null)
        {
            return false;
        }

        Vector3 world;
        try
        {
            var car = PlayerManager.Car ?? PlayerManager.LastLoco;
            var t = car != null ? car.transform : PlayerManager.PlayerTransform;
            if (t == null)
            {
                return false;
            }

            world = t.position;
        }
        catch
        {
            return false;
        }

        try
        {
            var curve = rail.curve;
            if (curve == null || curve.pointCount < 2)
            {
                return false;
            }

            var length = curve.length;
            if (length <= 1f)
            {
                length = LengthMetersOf(rail);
            }

            if (length <= 1f)
            {
                return false;
            }

            var closest = RailTrack.GetClosestPoint(rail, world, 0f);
            if (closest.Item1 is not { } point)
            {
                return false;
            }

            var span = (float)point.span;
            if (span < 0f)
            {
                span = 0f;
            }
            else if (span > length)
            {
                span = length;
            }

            var alongIncreasingSpan = true;
            if (_tracksByKey.TryGetValue(nextKey!, out var nextRail) && nextRail != null)
            {
                if (ReferenceEquals(rail.inBranch.track, nextRail)
                    || string.Equals(TrackKey(rail.inBranch.track), nextKey, System.StringComparison.Ordinal))
                {
                    alongIncreasingSpan = false;
                }
                else if (ReferenceEquals(rail.outBranch.track, nextRail)
                    || string.Equals(TrackKey(rail.outBranch.track), nextKey, System.StringComparison.Ordinal))
                {
                    alongIncreasingSpan = true;
                }
            }

            progress01 = alongIncreasingSpan ? span / length : 1f - (span / length);
            if (progress01 < 0f)
            {
                progress01 = 0f;
            }
            else if (progress01 > 1f)
            {
                progress01 = 1f;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Index of <paramref name="trackKey"/> (or alias) on the planned corridor, or -1.</summary>
    public static int CorridorIndex(PathPlanResult? plan, string? trackKey)
    {
        if (plan == null || plan.TrackIds.Count == 0)
        {
            return -1;
        }

        var exact = PathRouteDebug.IndexOfTrack(plan.TrackIds, trackKey);
        if (exact >= 0)
        {
            return exact;
        }

        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key) || _tracksByKey == null
            || !_tracksByKey.TryGetValue(key!, out var rail) || rail == null)
        {
            return -1;
        }

        for (var i = 0; i < plan.TrackIds.Count; i++)
        {
            var id = plan.TrackIds[i];
            if (id != null
                && _tracksByKey.TryGetValue(id, out var planRail)
                && ReferenceEquals(planRail, rail))
            {
                return i;
            }
        }

        return -1;
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
    /// Graph node id — prefer display (<c>FF-A1L</c>); FullID (<c>#…</c>) registered as alias.
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

    /// <summary>
    /// Expand occupancy keys with every alias of the same <see cref="RailTrack"/>
    /// (FullDisplayID ↔ FullID / <c>#Y</c>). Cars often report HB-*; edges use #Y-*.
    /// </summary>
    public static HashSet<string> ExpandOccupiedAliases(IEnumerable<string>? occupiedKeys)
    {
        var set = new HashSet<string>(System.StringComparer.Ordinal);
        if (occupiedKeys == null || _tracksByKey == null)
        {
            if (occupiedKeys != null)
            {
                foreach (var k in occupiedKeys)
                {
                    if (!string.IsNullOrWhiteSpace(k))
                    {
                        set.Add(k.Trim());
                    }
                }
            }

            return set;
        }

        foreach (var key in occupiedKeys)
        {
            var k = key?.Trim();
            if (string.IsNullOrEmpty(k))
            {
                continue;
            }

            set.Add(k!);
            if (!_tracksByKey.TryGetValue(k!, out var rail) || rail == null)
            {
                continue;
            }

            foreach (var kv in _tracksByKey)
            {
                if (ReferenceEquals(kv.Value, rail))
                {
                    set.Add(kv.Key);
                }
            }
        }

        return set;
    }

    /// <summary>
    /// Resolve every graph key, including <c>#Y</c> aliases, to its named yard when the
    /// same RailTrack also has a display id such as <c>HB-G3O</c>.
    /// </summary>
    public static Dictionary<string, string> BuildYardAliasMap()
    {
        var result = new Dictionary<string, string>(System.StringComparer.Ordinal);
        if (_tracksByKey == null)
        {
            return result;
        }

        var yardByRail = new Dictionary<RailTrack, string>();
        foreach (var kv in _tracksByKey)
        {
            var yard = PathRouteConstraints.YardIdOf(kv.Key);
            if (yard != null && kv.Value != null)
            {
                yardByRail[kv.Value] = yard;
            }
        }

        foreach (var kv in _tracksByKey)
        {
            if (kv.Value != null && yardByRail.TryGetValue(kv.Value, out var yard))
            {
                result[kv.Key] = yard;
            }
        }

        return result;
    }

    /// <summary>
    /// Graph keys tagged to <paramref name="yardId"/> for mini-map seeds (named + yard-tagged <c>#Y</c>).
    /// Prefer primary <see cref="TrackKey"/> so keys match edge nodes.
    /// </summary>
    public static List<string> CollectYardSeedTrackKeys(string? yardId)
    {
        var list = new List<string>(64);
        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard) || _tracksByKey == null)
        {
            return list;
        }

        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        var seenRails = new HashSet<RailTrack>();
        var aliasMap = BuildYardAliasMap();

        foreach (var kv in _tracksByKey)
        {
            var rail = kv.Value;
            if (rail == null || !seenRails.Add(rail))
            {
                continue;
            }

            var primary = PrimaryKeyOf(rail) ?? kv.Key;
            if (string.IsNullOrEmpty(primary))
            {
                continue;
            }

            var y = YardIdOf(rail)
                ?? YardIdFromTrackKey(primary)
                ?? (aliasMap.TryGetValue(primary!, out var ay) ? ay : null);
            if (!string.Equals(y, yard, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (seen.Add(primary!))
            {
                list.Add(primary!);
            }
        }

        return list;
    }

    /// <summary>Yard for a graph key (display prefix, rail LogicTrack, or alias map).</summary>
    public static string? ResolveTrackYard(string? trackKey)
    {
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        var fromKey = YardIdFromTrackKey(key);
        if (fromKey != null)
        {
            return fromKey;
        }

        if (TryGetRailTrack(key, out var rail) && rail != null)
        {
            var fromRail = YardIdOf(rail);
            if (fromRail != null)
            {
                return fromRail;
            }
        }

        // One-shot alias lookup (callers that BFS should cache BuildYardAliasMap themselves).
        var aliasMap = BuildYardAliasMap();
        return aliasMap.TryGetValue(key!, out var yard) ? yard : null;
    }

    /// <summary>
    /// True when <paramref name="trackKey"/> is on the plan, or an alias of a plan track
    /// (FullID ↔ FullDisplayID) mapped to the same <see cref="RailTrack"/>.
    /// </summary>
    public static bool IsOnPlannedCorridor(PathPlanResult? plan, string? trackKey)
    {
        if (plan == null)
        {
            return false;
        }

        if (plan.ContainsTrack(trackKey))
        {
            return true;
        }

        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key) || _tracksByKey == null)
        {
            return false;
        }

        if (!_tracksByKey.TryGetValue(key!, out var rail) || rail == null)
        {
            return false;
        }

        foreach (var kv in _tracksByKey)
        {
            if (ReferenceEquals(kv.Value, rail) && plan.ContainsTrack(kv.Key))
            {
                return true;
            }
        }

        return false;
    }

    private static void RegisterTrackKeys(
        Dictionary<string, RailTrack> tracksByKey,
        RailTrack track,
        string primaryKey)
    {
        tracksByKey[primaryKey] = track;
        foreach (var alias in AlternateTrackKeys(track, primaryKey))
        {
            tracksByKey[alias] = track;
        }
    }

    private static IEnumerable<string> AlternateTrackKeys(RailTrack track, string primaryKey)
    {
        string? display = null;
        string? full = null;
        try
        {
            var id = LogicTrackOf(track)?.ID;
            display = id?.FullDisplayID?.Trim();
            full = id?.FullID?.Trim();
        }
        catch
        {
            // ignore
        }

        if (!string.IsNullOrEmpty(display)
            && !string.Equals(display, primaryKey, System.StringComparison.Ordinal))
        {
            yield return display!;
        }

        if (!string.IsNullOrEmpty(full)
            && !string.Equals(full, primaryKey, System.StringComparison.Ordinal)
            && !string.Equals(full, display, System.StringComparison.Ordinal))
        {
            yield return full!;
        }
    }

    private static float LengthMetersOf(RailTrack track)
    {
        try
        {
            var curve = track.curve;
            if (curve != null && curve.pointCount >= 2)
            {
                var len = curve.length;
                if (len > 0f)
                {
                    return len;
                }

                var first = curve[0];
                var last = curve[curve.pointCount - 1];
                if (first != null && last != null)
                {
                    var chord = Vector3.Distance(first.position, last.position);
                    if (chord > 0f)
                    {
                        return chord;
                    }
                }
            }
        }
        catch
        {
            // fall through
        }

        return PathTrackCosts.MinLengthMeters;
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
        if (!TryGetRailTrack(trackKey, out var rail) || rail == null)
        {
            return false;
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

    /// <summary>Resolve a cached rail by primary track key (after mapping warm).</summary>
    public static bool TryGetRailTrack(string? trackKey, out RailTrack? rail)
    {
        rail = null;
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (_tracksByKey == null || !_tracksByKey.TryGetValue(key!, out rail) || rail == null)
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

        return rail != null;
    }

    /// <summary>
    /// Unique rails near world XZ (primary key per RailTrack), sorted by distance.
    /// Used to match the visual "straight ahead" slice to a graph id.
    /// </summary>
    public static List<(string TrackId, float DistM, PathTrackClass Cls)> CollectNearbyTracks(
        float worldX,
        float worldZ,
        float radiusMeters,
        int max = 48)
    {
        var list = new List<(string, float, PathTrackClass)>();
        if (_tracksByKey == null || radiusMeters <= 0f || max <= 0)
        {
            return list;
        }

        var seenRails = new HashSet<RailTrack>();
        var r2 = radiusMeters * radiusMeters;
        foreach (var kv in _tracksByKey)
        {
            var rail = kv.Value;
            if (rail == null || !seenRails.Add(rail))
            {
                continue;
            }

            float dx;
            float dz;
            try
            {
                var p = rail.transform.position;
                dx = p.x - worldX;
                dz = p.z - worldZ;
            }
            catch
            {
                continue;
            }

            var d2 = dx * dx + dz * dz;
            if (d2 > r2)
            {
                continue;
            }

            var key = PrimaryKeyOf(rail) ?? kv.Key;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            var cls = _metaByKey != null && _metaByKey.TryGetValue(key!, out var meta)
                ? meta.TrackClass
                : PathTrackClass.Unknown;
            list.Add((key!, Mathf.Sqrt(d2), cls));
        }

        list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        if (list.Count > max)
        {
            list.RemoveRange(max, list.Count - max);
        }

        return list;
    }

    private static string? PrimaryKeyOf(RailTrack rail)
    {
        try
        {
            return TrackKey(rail);
        }
        catch
        {
            return null;
        }
    }

    private static PathTrackClass ClassifyTrack(RailTrack rail)
    {
        try
        {
            var id = LogicTrackOf(rail)?.ID;
            if (id == null)
            {
                return PathTrackClass.Unknown;
            }

            var typeToken = TryReadTrackType(id) ?? id.FullID ?? id.FullDisplayID;
            return PathTrackCosts.Classify(typeToken);
        }
        catch
        {
            return PathTrackClass.Unknown;
        }
    }

    private static float? GeometryLimitKmh(RailTrack track)
    {
        try
        {
            // Permanent cache in TelemetryReader — survives InvalidateCache / Align.
            return TelemetryReader.GetOrComputeTrackGeometryLimitKmh(track);
        }
        catch
        {
            return null;
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

        // enterCost = travel seconds into `to`; junctions add switch slowdown.
        enterCost.TryGetValue(to, out var cost);
        if (cost <= 0f)
        {
            cost = PathTrackCosts.TravelSeconds(
                PathTrackCosts.MinLengthMeters,
                null,
                PathTrackClass.Unknown);
        }

        edges.Add(new PathEdge(
            from,
            to,
            junctionId,
            branch,
            cost + PathTrackCosts.JunctionPenaltySeconds));
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
            cost = PathTrackCosts.TravelSeconds(
                PathTrackCosts.MinLengthMeters,
                null,
                PathTrackClass.Unknown);
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
