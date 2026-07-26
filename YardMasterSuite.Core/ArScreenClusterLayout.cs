using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Pack on-screen AR markers so their AABBs never overlap — side-by-side squares, no Venn (4.10).
/// </summary>
public static class ArScreenClusterLayout
{
    public const float DefaultGapPixels = 8f;

    /// <summary>
    /// Mutates <paramref name="centerXs"/> so horizontal intervals
    /// <c>[x−halfW, x+halfW]</c> do not overlap (plus <paramref name="gapPixels"/>).
    /// Clusters pack left→right around the X centroid, then shift <b>as a rigid group</b> into
    /// <c>[edgeMargin, screenWidth−edgeMargin]</c> so edge clamp cannot recreate a Venn.
    /// </summary>
    public static void PackNonOverlapping(
        float[] centerXs,
        float[] centerYs,
        float[] halfWidths,
        float[] halfHeights,
        int count,
        float gapPixels,
        float screenWidth = 0f,
        float edgeMargin = 0f)
    {
        if (centerXs == null || centerYs == null || halfWidths == null || halfHeights == null)
        {
            throw new ArgumentNullException();
        }

        if (count <= 1)
        {
            return;
        }

        if (centerXs.Length < count || centerYs.Length < count
            || halfWidths.Length < count || halfHeights.Length < count)
        {
            throw new ArgumentException("Input arrays shorter than count.");
        }

        if (gapPixels < 0f)
        {
            gapPixels = 0f;
        }

        var parent = new int[count];
        for (var i = 0; i < count; i++)
        {
            parent[i] = i;
        }

        int Find(int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        void Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
            {
                parent[rb] = ra;
            }
        }

        for (var i = 0; i < count; i++)
        {
            for (var j = i + 1; j < count; j++)
            {
                if (BoxesOverlap(centerXs, centerYs, halfWidths, halfHeights, i, j, gapPixels))
                {
                    Union(i, j);
                }
            }
        }

        var members = new int[count];
        for (var root = 0; root < count; root++)
        {
            if (Find(root) != root)
            {
                continue;
            }

            var n = 0;
            for (var i = 0; i < count; i++)
            {
                if (Find(i) == root)
                {
                    members[n++] = i;
                }
            }

            if (n == 0)
            {
                continue;
            }

            if (n >= 2)
            {
                Array.Sort(members, 0, n, Comparer<int>.Create((a, b) =>
                {
                    var cmp = centerXs[a].CompareTo(centerXs[b]);
                    return cmp != 0 ? cmp : a.CompareTo(b);
                }));

                var sumX = 0f;
                var totalSpan = 0f;
                for (var i = 0; i < n; i++)
                {
                    var idx = members[i];
                    sumX += centerXs[idx];
                    totalSpan += halfWidths[idx] * 2f;
                    if (i < n - 1)
                    {
                        totalSpan += gapPixels;
                    }
                }

                var centroid = sumX / n;
                var cursor = centroid - (totalSpan * 0.5f);
                for (var i = 0; i < n; i++)
                {
                    var idx = members[i];
                    var hw = halfWidths[idx];
                    centerXs[idx] = cursor + hw;
                    cursor += (hw * 2f) + gapPixels;
                }
            }

            if (screenWidth > 0f)
            {
                FitClusterSpanInView(
                    centerXs,
                    halfWidths,
                    members,
                    n,
                    edgeMargin,
                    Math.Max(edgeMargin, screenWidth - edgeMargin));
            }
        }
    }

    /// <summary>
    /// Shift a packed cluster rigidly so its outer edges stay in <paramref name="viewMin"/>..<paramref name="viewMax"/>.
    /// Preserves center-to-center gaps (does not clamp each box independently).
    /// </summary>
    public static void FitClusterSpanInView(
        float[] centerXs,
        float[] halfWidths,
        int[] members,
        int memberCount,
        float viewMin,
        float viewMax)
    {
        if (centerXs == null || halfWidths == null || members == null || memberCount <= 0)
        {
            return;
        }

        if (viewMax < viewMin)
        {
            return;
        }

        var left = centerXs[members[0]] - halfWidths[members[0]];
        var right = centerXs[members[0]] + halfWidths[members[0]];
        for (var i = 1; i < memberCount; i++)
        {
            var idx = members[i];
            left = Math.Min(left, centerXs[idx] - halfWidths[idx]);
            right = Math.Max(right, centerXs[idx] + halfWidths[idx]);
        }

        var span = right - left;
        var avail = viewMax - viewMin;
        float shift;
        if (span >= avail)
        {
            shift = viewMin + (avail * 0.5f) - ((left + right) * 0.5f);
        }
        else
        {
            shift = 0f;
            if (left + shift < viewMin)
            {
                shift = viewMin - left;
            }

            if (right + shift > viewMax)
            {
                shift = viewMax - right;
            }
        }

        if (Math.Abs(shift) < 0.01f)
        {
            return;
        }

        for (var i = 0; i < memberCount; i++)
        {
            centerXs[members[i]] += shift;
        }
    }

    /// <summary>True when expanded AABBs touch or overlap.</summary>
    public static bool BoxesOverlap(
        float[] centerXs,
        float[] centerYs,
        float[] halfWidths,
        float[] halfHeights,
        int i,
        int j,
        float gapPixels)
    {
        var dx = Math.Abs(centerXs[i] - centerXs[j]);
        var dy = Math.Abs(centerYs[i] - centerYs[j]);
        var needX = halfWidths[i] + halfWidths[j] + gapPixels;
        var needY = halfHeights[i] + halfHeights[j] + gapPixels;
        return dx < needX && dy < needY;
    }

    /// <summary>Center-to-center distance required for non-overlapping boxes with gap.</summary>
    public static float RequiredCenterDistance(float halfWidthA, float halfWidthB, float gapPixels) =>
        halfWidthA + halfWidthB + Math.Max(0f, gapPixels);
}
