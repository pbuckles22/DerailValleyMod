using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One candidate intermediate stop for multi-leg Align (Town TT / 3.7 precursor).</summary>
public readonly struct RoutePivotCandidate
{
    public RoutePivotCandidate(
        string trackId,
        bool canReachFromOrigin,
        bool canReachFinal,
        float originToPivotCost,
        float metersToFinal)
    {
        TrackId = trackId;
        CanReachFromOrigin = canReachFromOrigin;
        CanReachFinal = canReachFinal;
        OriginToPivotCost = originToPivotCost;
        MetersToFinal = metersToFinal;
    }

    public string TrackId { get; }
    public bool CanReachFromOrigin { get; }
    public bool CanReachFinal { get; }
    public float OriginToPivotCost { get; }
    public float MetersToFinal { get; }
}

/// <summary>
/// Pick the first Align stop when origin→final is NoPath.
/// Prefer a pivot reachable from origin that can still reach final; else nearest pull-toward-final.
/// </summary>
public static class RouteFirstPivot
{
    public static string? Pick(
        string? originTrackId,
        string? finalTrackId,
        IReadOnlyList<RoutePivotCandidate>? candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        var origin = originTrackId?.Trim();
        var final = finalTrackId?.Trim();
        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(final))
        {
            return null;
        }

        string? bestBridge = null;
        var bestBridgeCost = float.PositiveInfinity;
        string? bestPull = null;
        var bestPullDist = float.PositiveInfinity;

        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var id = c.TrackId?.Trim();
            if (string.IsNullOrEmpty(id)
                || string.Equals(id, origin, StringComparison.Ordinal)
                || string.Equals(id, final, StringComparison.Ordinal))
            {
                continue;
            }

            if (!c.CanReachFromOrigin)
            {
                continue;
            }

            if (c.CanReachFinal)
            {
                if (c.OriginToPivotCost < bestBridgeCost)
                {
                    bestBridgeCost = c.OriginToPivotCost;
                    bestBridge = id;
                }
            }
            else if (c.MetersToFinal < bestPullDist)
            {
                bestPullDist = c.MetersToFinal;
                bestPull = id;
            }
        }

        return bestBridge ?? bestPull;
    }
}
