using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Build one-yard schematic data for the mini-map overlay (4.13): track polylines + office/TT landmarks.
/// Includes usable named rails and nearby anonymous <c>#Y</c> connectors.
/// Zoom + draw window = named + landmarks + nearby extras only (0.6.29 / 0.6.32 perf).
/// </summary>
internal static class YardMiniMapBuilder
{
    /// <summary>Extra margin around focus points (parent yards; job zone &gt; AABB).</summary>
    public const float BoundsPaddingMeters = 120f;

    /// <summary>Tight pad for satellite fence checks (MFMB) — must not reach Machine Factory apron.</summary>
    public const float SatelliteFencePaddingMeters = 20f;

    public sealed class Snapshot
    {
        public string YardId = "";
        public float MinX;
        public float MaxX;
        public float MinZ;
        public float MaxZ;
        public readonly List<(float X, float Z)[]> Polylines = new();
        public bool HasOffice;
        public float OfficeX;
        public float OfficeZ;
        public readonly List<(float X, float Z)> Turntables = new();
        public int NamedRailCount;
        public int ExtraRailCount;
        public float FocusSpanMeters;
    }

    public static bool TryBuild(string yardId, out Snapshot? snapshot) =>
        TryBuild(yardId, BoundsPaddingMeters, out snapshot);

    public static bool TryBuild(string yardId, float paddingMeters, out Snapshot? snapshot)
    {
        snapshot = null;
        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard))
        {
            return false;
        }

        if (!PathGraphBuilder.TryBuild(out var edges, out _, out _, out var catalog) || catalog == null)
        {
            return false;
        }

        var namedSeedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seeds = new List<string>(64);
        var seenSeed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in DestinationCatalog.ListTracksInYard(catalog, yard))
        {
            namedSeedIds.Add(id);
            if (seenSeed.Add(id))
            {
                seeds.Add(id);
            }
        }

        foreach (var id in PathGraphBuilder.CollectYardSeedTrackKeys(yard))
        {
            if (seenSeed.Add(id))
            {
                seeds.Add(id);
            }

            namedSeedIds.Add(id);
        }

        if (seeds.Count == 0)
        {
            return false;
        }

        var usable = YardMiniMapTrackSet.CollectUsableTrackKeys(
            yard,
            seeds,
            edges,
            MakeYardResolver());

        if (usable.Count == 0)
        {
            return false;
        }

        var namedPoints = new List<(float X, float Z)>(256);
        var namedPolys = new List<(float X, float Z)[]>(64);
        var extraCandidates = new List<(float X, float Z)[]>(128);
        var landmarks = new List<(float X, float Z)>(8);
        var seenRails = new HashSet<RailTrack>();
        var namedCount = 0;

        // Pass 1: sample named/yard-tagged rails (focus seeds).
        foreach (var key in usable)
        {
            if (!IsNamedFocusKey(key, namedSeedIds))
            {
                continue;
            }

            if (!TrySampleUniqueRail(key, seenRails, out var poly) || poly == null)
            {
                continue;
            }

            namedCount++;
            namedPolys.Add(poly);
            namedPoints.AddRange(poly);
        }

        if (TelemetryReader.TryGetOfficeForYard(yard, out var ox, out var oz))
        {
            landmarks.Add((ox, oz));
        }

        var turntables = new List<(float X, float Z)>(4);
        CollectTurntables(yard!, turntables);
        landmarks.AddRange(turntables);

        if (namedPoints.Count == 0 && landmarks.Count == 0)
        {
            return false;
        }

        // Focus window from named + landmarks (no distant #Y inflate).
        var focusSeed = YardMiniMapSchematicFocus.CollectFocusPoints(
            namedPoints,
            extraPoints: null,
            landmarks,
            YardMiniMapSchematicFocus.DefaultExtraIncludeMeters);

        if (!YardMiniMapProjection.TryFitBounds(
                focusSeed,
                paddingMeters,
                out var minX,
                out var maxX,
                out var minZ,
                out var maxZ))
        {
            return false;
        }

        // Expand slightly for nearby #Y path lines without city-wide draw.
        var drawMinX = minX - YardMiniMapSchematicFocus.DefaultExtraIncludeMeters;
        var drawMaxX = maxX + YardMiniMapSchematicFocus.DefaultExtraIncludeMeters;
        var drawMinZ = minZ - YardMiniMapSchematicFocus.DefaultExtraIncludeMeters;
        var drawMaxZ = maxZ + YardMiniMapSchematicFocus.DefaultExtraIncludeMeters;

        // Pass 2: sample anonymous extras only if they intersect the draw window.
        var extraCount = 0;
        foreach (var key in usable)
        {
            if (IsNamedFocusKey(key, namedSeedIds))
            {
                continue;
            }

            if (!TrySampleUniqueRail(key, seenRails, out var poly) || poly == null)
            {
                continue;
            }

            if (!YardMiniMapRebuildGate.PolylineIntersectsBounds(
                    poly, drawMinX, drawMaxX, drawMinZ, drawMaxZ))
            {
                continue;
            }

            extraCount++;
            extraCandidates.Add(poly);
        }

        var snap = new Snapshot
        {
            YardId = yard!,
            MinX = minX,
            MaxX = maxX,
            MinZ = minZ,
            MaxZ = maxZ,
            NamedRailCount = namedCount,
            ExtraRailCount = extraCount,
        };

        if (TelemetryReader.TryGetOfficeForYard(yard, out ox, out oz))
        {
            snap.HasOffice = true;
            snap.OfficeX = ox;
            snap.OfficeZ = oz;
        }

        snap.Turntables.AddRange(turntables);
        snap.Polylines.AddRange(namedPolys);
        snap.Polylines.AddRange(extraCandidates);

        var spanX = snap.MaxX - snap.MinX;
        var spanZ = snap.MaxZ - snap.MinZ;
        snap.FocusSpanMeters = spanX > spanZ ? spanX : spanZ;

        snapshot = snap;
        return true;
    }

    /// <summary>True when player XZ lies inside the yard fence/schematic AABB.</summary>
    public static bool IsInsideFootprint(string yardId, float worldX, float worldZ)
    {
        var pad = YardMiniMapYardStick.IsSatelliteYard(yardId)
            ? SatelliteFencePaddingMeters
            : BoundsPaddingMeters;
        if (!TryBuild(yardId, pad, out var snap) || snap == null)
        {
            return false;
        }

        return !YardMiniMapProjection.IsOutsideBounds(
            worldX,
            worldZ,
            snap.MinX,
            snap.MaxX,
            snap.MinZ,
            snap.MaxZ);
    }

    private static bool TrySampleUniqueRail(
        string key,
        HashSet<RailTrack> seenRails,
        out (float X, float Z)[]? poly)
    {
        poly = null;
        if (!PathGraphBuilder.TryGetRailTrack(key, out var rail) || rail == null)
        {
            return false;
        }

        if (!seenRails.Add(rail))
        {
            return false;
        }

        var points = new List<(float X, float Z)>(16);
        if (!YardTrackGeometry.TrySampleTrackXZ(rail, points) || points.Count == 0)
        {
            return false;
        }

        poly = points.ToArray();
        return true;
    }

    private static bool IsNamedFocusKey(string key, HashSet<string> namedSeedIds)
    {
        if (namedSeedIds.Contains(key))
        {
            return true;
        }

        if (!PathGraphBuilder.TryGetRailTrack(key, out var rail) || rail == null)
        {
            return false;
        }

        var primary = PathGraphBuilder.TrackKey(rail);
        return primary != null && namedSeedIds.Contains(primary);
    }

    private static Func<string, string?> MakeYardResolver()
    {
        var aliasMap = PathGraphBuilder.BuildYardAliasMap();
        return key =>
        {
            var fromKey = PathGraphBuilder.YardIdFromTrackKey(key);
            if (fromKey != null)
            {
                return fromKey;
            }

            if (aliasMap.TryGetValue(key, out var aliased))
            {
                return aliased;
            }

            if (PathGraphBuilder.TryGetRailTrack(key, out var rail) && rail != null)
            {
                return PathGraphBuilder.YardIdOf(rail);
            }

            return null;
        };
    }

    private static void CollectTurntables(string yardId, List<(float X, float Z)> into)
    {
        try
        {
            var tables = UnityEngine.Object.FindObjectsOfType<TurntableController>();
            if (tables == null || tables.Length == 0)
            {
                return;
            }

            for (var i = 0; i < tables.Length; i++)
            {
                var ctrl = tables[i];
                if (ctrl == null || ctrl.turntable == null)
                {
                    continue;
                }

                RailTrack? rail = null;
                try
                {
                    rail = ctrl.turntable.Track;
                }
                catch
                {
                    rail = null;
                }

                var key = PathGraphBuilder.TrackKey(rail);
                var yard = PathGraphBuilder.YardIdOf(rail)
                    ?? PathGraphBuilder.YardIdFromTrackKey(key);
                if (!string.Equals(yard, yardId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Vector3 p;
                try
                {
                    p = rail != null ? rail.transform.position : ctrl.transform.position;
                }
                catch
                {
                    continue;
                }

                into.Add((p.x, p.z));
            }
        }
        catch
        {
            // fail-closed: no TT landmarks
        }
    }
}
