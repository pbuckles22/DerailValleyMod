using System.Collections.Generic;
using System.Text;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Hybrid FoT → Core resolver. Call only from intentional desk / Align paths — never HUD tick.
/// </summary>
internal static class TurntableLocator
{
    /// <summary>
    /// Finds turntable rails for <paramref name="yardId"/> (prefer yard meta; else nearest in town).
    /// </summary>
    public static string? TryResolveTrackId(string yardId, float originX, float originZ)
    {
        if (string.IsNullOrWhiteSpace(yardId))
        {
            return null;
        }

        List<TurntableCandidate>? candidates;
        var skippedNoKey = 0;
        try
        {
            var tables = UnityEngine.Object.FindObjectsOfType<TurntableController>();
            if (tables == null || tables.Length == 0)
            {
                Main.Log("T2 path: TT FoT count=0");
                return null;
            }

            candidates = new List<TurntableCandidate>(tables.Length);
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
                if (string.IsNullOrWhiteSpace(key))
                {
                    skippedNoKey++;
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

                // Blank yard is OK — infer from nearest named rail, else Core fallback (same town only).
                var yard = PathGraphBuilder.YardIdOf(rail)
                    ?? PathGraphBuilder.YardIdFromTrackKey(key)
                    ?? InferYardNear(p)
                    ?? "";

                var dx = p.x - originX;
                var dz = p.z - originZ;
                var dist = Mathf.Sqrt(dx * dx + dz * dz);
                candidates.Add(new TurntableCandidate(key!, yard, dist));
            }

            Main.Log(FormatCandidateDiag(yardId, tables.Length, skippedNoKey, candidates));
        }
        catch
        {
            return null;
        }

        // Never fall back playerYard to target city — that re-enabled CME→SW steal.
        return TurntableTrackResolver.PickBest(
            yardId,
            candidates,
            TurntableTrackResolver.DefaultNearestFallbackMaxMeters,
            playerYardId: StickyYardHost.CurrentYardId);
    }

    private static string FormatCandidateDiag(
        string yardId,
        int fotCount,
        int skippedNoKey,
        List<TurntableCandidate> candidates)
    {
        var sb = new StringBuilder(128);
        sb.Append("T2 path: TT FoT=")
            .Append(fotCount)
            .Append(" cand=")
            .Append(candidates.Count)
            .Append(" noKey=")
            .Append(skippedNoKey)
            .Append(" want=")
            .Append(yardId);
        var n = Mathf.Min(candidates.Count, 6);
        for (var i = 0; i < n; i++)
        {
            var c = candidates[i];
            sb.Append(" | ")
                .Append(string.IsNullOrEmpty(c.YardId) ? "—" : c.YardId)
                .Append(':')
                .Append(c.TrackId)
                .Append('@')
                .Append(c.DistanceMeters.ToString("0"));
        }

        return sb.ToString();
    }

    /// <summary>Nearest named yard within 200 m of the turntable (blank bridge meta).</summary>
    private static string? InferYardNear(Vector3 world)
    {
        try
        {
            if (!PathGraphBuilder.HasReadyCache)
            {
                return null;
            }

            var near = PathGraphBuilder.CollectNearbyTracks(world.x, world.z, radiusMeters: 200f, max: 24);
            for (var i = 0; i < near.Count; i++)
            {
                var id = near[i].TrackId;
                var yard = PathGraphBuilder.YardIdFromTrackKey(id)
                    ?? PathRouteConstraints.YardIdOf(id);
                if (!string.IsNullOrWhiteSpace(yard)
                    && !yard!.StartsWith("#", System.StringComparison.Ordinal))
                {
                    return yard;
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
