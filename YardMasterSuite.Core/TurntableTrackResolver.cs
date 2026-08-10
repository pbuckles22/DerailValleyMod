using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure DTO for a discovered turntable (Unity maps controllers → this).
/// </summary>
public readonly struct TurntableCandidate
{
    public TurntableCandidate(string trackId, string yardId, float distanceMeters)
    {
        TrackId = trackId;
        YardId = yardId;
        DistanceMeters = distanceMeters;
    }

    public string TrackId { get; }
    public string YardId { get; }
    public float DistanceMeters { get; }
}

/// <summary>
/// Pick the turntable track for a yard (3.7 precursor / shared core).
/// Prefer yard match + nearest tie-break; optional in-town nearest fallback for blank/#Y yard meta.
/// </summary>
public static class TurntableTrackResolver
{
    /// <summary>Default town radius when yard meta is missing (SW TT smoke).</summary>
    public const float DefaultNearestFallbackMaxMeters = 500f;

    /// <summary>
    /// Selects the turntable belonging to <paramref name="targetYardId"/>.
    /// If multiple exist, picks the closest by <see cref="TurntableCandidate.DistanceMeters"/>.
    /// Nearest blank-yard fallback only when <paramref name="playerYardId"/> matches
    /// <paramref name="targetYardId"/> (do not steal SW TT when City=CME).
    /// </summary>
    public static string? PickBest(
        string? targetYardId,
        IReadOnlyList<TurntableCandidate>? candidates,
        float? nearestFallbackMaxMeters = null,
        string? playerYardId = null)
    {
        if (candidates == null || candidates.Count == 0 || string.IsNullOrWhiteSpace(targetYardId))
        {
            return null;
        }

        string? bestId = null;
        var bestDist = float.PositiveInfinity;

        for (var i = 0; i < candidates.Count; i++)
        {
            var cand = candidates[i];
            if (string.IsNullOrWhiteSpace(cand.TrackId))
            {
                continue;
            }

            if (!string.Equals(cand.YardId, targetYardId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (cand.DistanceMeters < bestDist)
            {
                bestDist = cand.DistanceMeters;
                bestId = cand.TrackId;
            }
        }

        if (bestId != null)
        {
            return bestId;
        }

        if (nearestFallbackMaxMeters == null)
        {
            return null;
        }

        // Blank/#Y TT meta: only for the town you are actually in.
        if (string.IsNullOrWhiteSpace(playerYardId)
            || string.IsNullOrWhiteSpace(targetYardId))
        {
            return null;
        }

        var player = playerYardId!.Trim();
        var target = targetYardId!.Trim();
        if (!string.Equals(player, target, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var max = nearestFallbackMaxMeters.Value;
        if (max < 0f)
        {
            return null;
        }

        bestId = null;
        bestDist = float.PositiveInfinity;
        for (var i = 0; i < candidates.Count; i++)
        {
            var cand = candidates[i];
            if (string.IsNullOrWhiteSpace(cand.TrackId) || cand.DistanceMeters > max)
            {
                continue;
            }

            if (cand.DistanceMeters < bestDist)
            {
                bestDist = cand.DistanceMeters;
                bestId = cand.TrackId;
            }
        }

        return bestId;
    }
}
