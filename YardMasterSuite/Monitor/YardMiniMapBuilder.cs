using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Build one-yard schematic data for the mini-map overlay (4.13): track polylines + office/TT landmarks.
/// </summary>
internal static class YardMiniMapBuilder
{
    /// <summary>Extra margin around named tracks + landmarks (parent yards; job zone &gt; AABB).</summary>
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

        if (!PathGraphBuilder.TryBuild(out _, out _, out _, out var catalog) || catalog == null)
        {
            return false;
        }

        var trackIds = DestinationCatalog.ListTracksInYard(catalog, yard);
        if (trackIds.Count == 0)
        {
            return false;
        }

        var allPoints = new List<(float X, float Z)>(256);
        var snap = new Snapshot { YardId = yard! };

        for (var i = 0; i < trackIds.Count; i++)
        {
            if (!PathGraphBuilder.TryGetRailTrack(trackIds[i], out var rail) || rail == null)
            {
                continue;
            }

            var poly = new List<(float X, float Z)>(16);
            if (!YardTrackGeometry.TrySampleTrackXZ(rail, poly) || poly.Count == 0)
            {
                continue;
            }

            snap.Polylines.Add(poly.ToArray());
            allPoints.AddRange(poly);
        }

        if (TelemetryReader.TryGetOfficeForYard(yard, out var ox, out var oz))
        {
            snap.HasOffice = true;
            snap.OfficeX = ox;
            snap.OfficeZ = oz;
            allPoints.Add((ox, oz));
        }

        CollectTurntables(yard!, snap.Turntables);
        for (var i = 0; i < snap.Turntables.Count; i++)
        {
            allPoints.Add(snap.Turntables[i]);
        }

        if (allPoints.Count == 0)
        {
            return false;
        }

        if (!YardMiniMapProjection.TryFitBounds(
                allPoints,
                paddingMeters,
                out snap.MinX,
                out snap.MaxX,
                out snap.MinZ,
                out snap.MaxZ))
        {
            return false;
        }

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
