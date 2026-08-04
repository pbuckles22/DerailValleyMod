using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One spawned loco candidate for radar ranking (4.10). Pure — no Unity.</summary>
public readonly struct LocoRadarCandidate
{
    public LocoRadarCandidate(int id, float distanceSq)
    {
        Id = id;
        DistanceSq = distanceSq;
    }

    public int Id { get; }
    public float DistanceSq { get; }
}

/// <summary>
/// Rank nearest other locomotives for AR radar (4.10).
/// Callers supply distance² and exclusion ids (self / my-loco AR target / same consist).
/// </summary>
public static class LocoRadarSelection
{
    public const int DefaultMaxResults = 3;

    /// <summary>Yard-walk useful range — farther markers are noise.</summary>
    public const float MaxRangeMeters = 600f;

    public static float MaxRangeDistanceSq => MaxRangeMeters * MaxRangeMeters;

    /// <summary>
    /// Writes nearest-first ids into <paramref name="rankedIds"/> (up to its length and
    /// <paramref name="maxResults"/>). Skips ids present in <paramref name="excludeIds"/>
    /// and candidates beyond <see cref="MaxRangeMeters"/>.
    /// </summary>
    public static int RankNearest(
        IReadOnlyList<LocoRadarCandidate> candidates,
        ICollection<int>? excludeIds,
        int maxResults,
        int[] rankedIds)
    {
        if (rankedIds == null || rankedIds.Length == 0 || maxResults <= 0
            || candidates == null || candidates.Count == 0)
        {
            return 0;
        }

        var cap = Math.Min(maxResults, rankedIds.Length);
        var maxSq = MaxRangeDistanceSq;
        // Insertion into a small top-N buffer (N ≤ 3 typical) — avoids LINQ alloc in hot path.
        var bestIds = new int[cap];
        var bestDist = new float[cap];
        var filled = 0;

        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (excludeIds != null && excludeIds.Contains(c.Id))
            {
                continue;
            }

            if (c.DistanceSq > maxSq || float.IsNaN(c.DistanceSq) || c.DistanceSq < 0f)
            {
                continue;
            }

            if (filled < cap)
            {
                InsertSorted(bestIds, bestDist, ref filled, c.Id, c.DistanceSq);
                continue;
            }

            if (c.DistanceSq >= bestDist[filled - 1])
            {
                continue;
            }

            filled--;
            InsertSorted(bestIds, bestDist, ref filled, c.Id, c.DistanceSq);
        }

        for (var i = 0; i < filled; i++)
        {
            rankedIds[i] = bestIds[i];
        }

        return filled;
    }

    private static void InsertSorted(int[] ids, float[] dist, ref int filled, int id, float distanceSq)
    {
        var i = filled;
        while (i > 0 && distanceSq < dist[i - 1])
        {
            ids[i] = ids[i - 1];
            dist[i] = dist[i - 1];
            i--;
        }

        ids[i] = id;
        dist[i] = distanceSq;
        filled++;
    }
}
