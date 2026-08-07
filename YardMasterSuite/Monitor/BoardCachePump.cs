using System.Collections.Generic;
using System.Diagnostics;
using DV.Signs;
using UnityEngine;
using YardMasterSuite.Core;
using Object = UnityEngine.Object;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Align-gated board cache: attach at most 4–8 on-route signs sync before Align returns OK
/// (budget ≤500 ms). Uses GetClosestPoint on planned rails only.
/// </summary>
internal sealed class BoardCachePump : MonoBehaviour
{
    private const float BoardTrackAttachMeters = 12f;
    private const float PathRailProximityMeters = 120f;

    private static BoardCachePump? _instance;
    private readonly HashSet<int> _processedSignIds = new();

    public static WorldSpeedBoardIndex Index { get; } = new();

    public static bool IsWarmed { get; private set; }

    public static bool IsWarming => false;

    public static bool HasTrackCacheReady =>
        IsWarmed || TelemetryReader.SessionBoardTrackCacheCount > 0;

    public static void EnsureStarted()
    {
        // No full-map warm.
    }

    public static void EnsureStartedFromSigns(SignDebug[] signs)
    {
        var plan = RoutePlanSession.Plan;
        if (plan == null || plan.TrackIds.Count == 0)
        {
            return;
        }

        WarmForPlan(plan, signs);
    }

    public static void NotifyTopologyReady()
    {
    }

    /// <summary>
    /// Sync warm: blocks until ≤8 on-route boards attached or 500 ms elapsed.
    /// Align calls this before returning OK to the player.
    /// </summary>
    public static void WarmForPlan(PathPlanResult plan, SignDebug[]? preloadedSigns = null)
    {
        if (plan.TrackIds.Count == 0)
        {
            return;
        }

        EnsureHost();
        var pathRails = ResolvePathRails(plan);
        if (pathRails.Count == 0)
        {
            Main.Log("T2 board-cache: align warm skip (no rail instances for plan)");
            return;
        }

        var signs = preloadedSigns;
        if (signs == null || signs.Length == 0)
        {
            signs = TelemetryReader.PeekSignDebugCache();
        }

        if (signs == null || signs.Length == 0)
        {
            try
            {
                signs = Object.FindObjectsOfType<SignDebug>() ?? System.Array.Empty<SignDebug>();
            }
            catch
            {
                signs = System.Array.Empty<SignDebug>();
            }

            TelemetryReader.AdoptSignDebugCache(signs);
        }

        if (signs.Length == 0)
        {
            Main.Log("T2 board-cache: align warm skip (no SignDebug)");
            return;
        }

        var sw = Stopwatch.StartNew();
        var attached = AttachOnRouteSync(signs, pathRails, sw);
        sw.Stop();
        IsWarmed = attached > 0;
        Main.Log(
            $"T2 board-cache: align sync attached={attached} in {sw.ElapsedMilliseconds}ms "
            + $"(cap={BoardCacheWarmPolicy.MaxOnRouteSigns}, budget={BoardCacheWarmPolicy.AlignBudgetMilliseconds}ms, "
            + $"complete={BoardCacheWarmPolicy.AlignWarmComplete(attached)})");
    }

    public static void ResetSession()
    {
        Index.Clear();
        IsWarmed = false;
        _instance?._processedSignIds.Clear();
        TelemetryReader.ClearSessionBoardTrackCache();
    }

    private static void EnsureHost()
    {
        if (_instance != null)
        {
            return;
        }

        var go = new GameObject("YMS_BoardCachePump");
        Object.DontDestroyOnLoad(go);
        _instance = go.AddComponent<BoardCachePump>();
    }

    private static List<RailTrack> ResolvePathRails(PathPlanResult plan)
    {
        var rails = new List<RailTrack>(plan.TrackIds.Count);
        var seen = new HashSet<int>();
        for (var i = 0; i < plan.TrackIds.Count; i++)
        {
            var key = plan.TrackIds[i];
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            if (!PathGraphBuilder.TryGetRailTrack(key!, out var rail) || rail == null)
            {
                continue;
            }

            var id = rail.GetInstanceID();
            if (seen.Add(id))
            {
                rails.Add(rail);
            }
        }

        return rails;
    }

    private static int AttachOnRouteSync(SignDebug[] signs, List<RailTrack> pathRails, Stopwatch sw)
    {
        var attached = 0;
        var prox2 = PathRailProximityMeters * PathRailProximityMeters;
        for (var i = 0; i < signs.Length; i++)
        {
            if (!BoardCacheWarmPolicy.ContinueAlignAttach(attached, sw.ElapsedMilliseconds))
            {
                break;
            }

            var sign = signs[i];
            if (sign == null)
            {
                continue;
            }

            var signId = sign.GetInstanceID();
            if (_instance!._processedSignIds.Contains(signId))
            {
                continue;
            }

            if (!IsNearAnyPathRail(sign.transform.position, pathRails, prox2))
            {
                continue;
            }

            if (TryAttachToPathRails(sign, signId, pathRails))
            {
                attached++;
            }
        }

        return attached;
    }

    private static bool IsNearAnyPathRail(Vector3 pos, List<RailTrack> pathRails, float proxSqr)
    {
        for (var i = 0; i < pathRails.Count; i++)
        {
            var rail = pathRails[i];
            if (rail == null)
            {
                continue;
            }

            try
            {
                var d = rail.transform.position - pos;
                if (d.sqrMagnitude <= proxSqr)
                {
                    return true;
                }
            }
            catch
            {
                // ignore
            }
        }

        return false;
    }

    private static bool TryAttachToPathRails(SignDebug sign, int signId, List<RailTrack> pathRails)
    {
        try
        {
            var pos = sign.transform.position;
            for (var i = 0; i < pathRails.Count; i++)
            {
                var rail = pathRails[i];
                if (rail == null)
                {
                    continue;
                }

                var pointInfo = RailTrack.GetClosestPoint(rail, pos, 0f);
                if (pointInfo.Item1 is not { } point
                    || pointInfo.Item2 > BoardTrackAttachMeters)
                {
                    continue;
                }

                TelemetryReader.RememberBoardTrack(signId, rail);
                _instance!._processedSignIds.Add(signId);

                var dual = SpeedLimitBoardParser.ParseDual(sign.text);
                if (dual is null)
                {
                    return true;
                }

                var tangent = point.forward;
                var governsForward = GovernsForward(sign, tangent);
                var travel = governsForward ? tangent : -tangent;
                var trackId = rail.GetInstanceID();
                var span = (float)point.span;
                var world = pos;

                Index.AddZone(trackId, span, dual.Value.ThroughKmh, governsForward);
                Index.Remember(
                    trackId,
                    dual.Value.ThroughKmh,
                    world.x,
                    world.y,
                    world.z,
                    travel.x,
                    travel.z);

                if (dual.Value.DivergeKmh is float diverge)
                {
                    Index.AddZone(trackId, span, diverge, governsForward);
                }

                return true;
            }

            _instance!._processedSignIds.Add(signId);
            return false;
        }
        catch
        {
            _instance!._processedSignIds.Add(signId);
            return false;
        }
    }

    private static bool GovernsForward(SignDebug sign, Vector3 trackTangent)
    {
        try
        {
            var f = sign.transform.forward;
            var tx = trackTangent.x;
            var tz = trackTangent.z;
            var len = Mathf.Sqrt((tx * tx) + (tz * tz));
            if (len < 1e-4f)
            {
                return true;
            }

            tx /= len;
            tz /= len;
            var fx = f.x;
            var fz = f.z;
            var fl = Mathf.Sqrt((fx * fx) + (fz * fz));
            if (fl < 1e-4f)
            {
                return true;
            }

            fx /= fl;
            fz /= fl;
            return (fx * tx) + (fz * tz) >= 0f;
        }
        catch
        {
            return true;
        }
    }
}
