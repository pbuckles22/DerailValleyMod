using System.Collections.Generic;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// ThreeGate Align Route — throws junctions for an explicitly computed plan (3.5).
/// Gated on <see cref="GeneralLicenseType.Dispatcher1"/>.
/// </summary>
internal static class RouteAlignGovernor
{
    public static string? TryAlign()
    {
        if (!RouteAlignAccess.CanAlign(HasDispatcherLicense()))
        {
            return "T2 align: need Dispatcher";
        }

        if (!RouteDestSession.HasDestination)
        {
            return "T2 align: no destination";
        }

        // Explicit compute (or memo) at click time only.
        var compute = RoutePlanService.Compute("align");
        Main.Log(compute);

        var plan = RoutePlanSession.Plan;
        if (plan == null
            || plan.Status == PathCheckStatus.NoPath
            || plan.Status == PathCheckStatus.NoOrigin)
        {
            // Prefer the compute line (e.g. no origin) over a vague "no path".
            return string.IsNullOrEmpty(compute) ? "T2 align: no path" : compute;
        }

        if (!PathGraphBuilder.TryBuild(out _, out _, out var junctionsById, out _))
        {
            return "T2 align: no graph";
        }

        var flips = PathPlan.RequiredFlips(plan);
        if (flips.Count == 0)
        {
            BoardCachePump.WarmForPlan(plan);
            return "T2 align: already clear";
        }

        var applied = 0;
        foreach (var flip in flips)
        {
            if (!junctionsById.TryGetValue(flip.JunctionId, out var junction) || junction == null)
            {
                return $"T2 align: abort unknown junction {flip.JunctionId}";
            }

            var branch = flip.RequiredBranch;
            if (branch < 0 || branch > 255)
            {
                return "T2 align: abort bad branch";
            }

            var result = ThreeGate.TryApply(
                integrityOk: true,
                stateRegistryOk: junction.outBranches != null,
                safetyOk: true,
                softWrite: () =>
                {
                    junction.Switch(Junction.SwitchMode.REGULAR, (byte)branch);
                    return true;
                });

            if (!result.Applied)
            {
                return $"T2 align: abort {result.AbortReason} @ {flip.JunctionId}";
            }

            applied++;
        }

        // Do NOT InvalidateCache here — that forced a full ~2k-track Rebuild hitch.
        // Topology/lengths unchanged after flips; TryBuild refreshes selectedBranch only.
        RouteMemo.Clear();
        // Re-eval the same corridor (do not re-Dijkstra — origin churn was causing Path-wrong).
        Main.Log(RoutePlanService.ReevaluateAfterAlign(plan));
        // Block up to ~0.5 s attaching ≤8 on-route boards before telling the player OK.
        BoardCachePump.WarmForPlan(plan);
        return $"T2 align: threw {applied}";
    }

    public static bool HasDispatcherLicense()
    {
        try
        {
            var lm = LicenseManager.Instance;
            if (lm == null)
            {
                return false;
            }

            var v2 = TransitionHelpers.ToV2(GeneralLicenseType.Dispatcher1);
            return v2 != null && lm.IsGeneralLicenseAcquired(v2);
        }
        catch
        {
            return false;
        }
    }
}

internal static class TelemetryReaderOrigin
{
    /// <summary>Max distance (m) from player feet to count as "on a track".</summary>
    private const float MaxPlayerTrackMeters = 12f;

    public static string? TryGet()
    {
        try
        {
            // Boarded car first (actual seat), then standing-on car, then feet on rails.
            // LastLoco is last — it can be far away while walking the yard.
            var key = FromCar(PlayerManager.Car)
                ?? FromCar(TelemetryReader.TryGetStandingCar())
                ?? FromPlayerPosition()
                ?? FromCar(PlayerManager.LastLoco);
            return key;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Logic + bogie keys (can disagree at junctions). Drift stays Path OK if any is on-route.
    /// </summary>
    public static List<string> TryGetCandidates()
    {
        var list = new List<string>(6);
        try
        {
            AddCarCandidates(list, PlayerManager.Car);
            AddCarCandidates(list, TelemetryReader.TryGetStandingCar());
            var feet = FromPlayerPosition();
            if (feet != null)
            {
                AddUnique(list, feet);
            }

            AddCarCandidates(list, PlayerManager.LastLoco);
        }
        catch
        {
            // keep whatever we collected
        }

        return list;
    }

    private static void AddCarCandidates(List<string> list, TrainCar? car)
    {
        if (car == null)
        {
            return;
        }

        try
        {
            AddUnique(list, PathGraphBuilder.TrackKey(car.logicCar?.CurrentTrack));
            AddUnique(list, PathGraphBuilder.TrackKey(car.FrontBogie?.track));
            AddUnique(list, PathGraphBuilder.TrackKey(car.RearBogie?.track));
        }
        catch
        {
            // ignore this car
        }
    }

    private static void AddUnique(List<string> list, string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], key, System.StringComparison.Ordinal))
            {
                return;
            }
        }

        list.Add(key!);
    }

    private static string? FromCar(TrainCar? car)
    {
        if (car == null)
        {
            return null;
        }

        try
        {
            var fromLogic = PathGraphBuilder.TrackKey(car.logicCar?.CurrentTrack);
            if (fromLogic != null)
            {
                return fromLogic;
            }

            var bogie = car.FrontBogie ?? car.RearBogie;
            return PathGraphBuilder.TrackKey(bogie?.track);
        }
        catch
        {
            return null;
        }
    }

    private static string? FromPlayerPosition()
    {
        var player = PlayerManager.PlayerTransform;
        if (player == null)
        {
            return null;
        }

        var tracks = RailTrackRegistry.RailTracks;
        if (tracks == null || tracks.Length == 0)
        {
            return null;
        }

        var closest = RailTrack.GetClosest(player.position, 0f, tracks);
        var rail = closest.Item1;
        if (rail == null)
        {
            return null;
        }

        var pointDist = RailTrack.GetClosestPoint(rail, player.position, 0f);
        if (pointDist.Item2 > MaxPlayerTrackMeters)
        {
            return null;
        }

        return PathGraphBuilder.TrackKey(rail);
    }
}
