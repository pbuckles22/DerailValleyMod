using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Fan sticky markers that share a left/right edge so they do not stack on one pixel (A.3).
/// Outermost = furthest from camera center; peel order matches turn-in.
/// </summary>
public static class ArEdgeStackLayout
{
    public const float DefaultSeparationPixels = 40f;
    public const float EdgeDetectTolerancePixels = 2.5f;

    public static ArHorizontalEdge DetectEdge(
        float screenX,
        float screenWidth,
        float edgeMargin,
        float tolerancePixels = EdgeDetectTolerancePixels)
    {
        if (Math.Abs(screenX - edgeMargin) <= tolerancePixels)
        {
            return ArHorizontalEdge.Left;
        }

        var rightX = Math.Max(edgeMargin, screenWidth - edgeMargin);
        if (Math.Abs(screenX - rightX) <= tolerancePixels)
        {
            return ArHorizontalEdge.Right;
        }

        return ArHorizontalEdge.None;
    }

    /// <summary>
    /// Higher sort key = more extreme outward on this edge (left: more negative bearing; right: more positive).
    /// </summary>
    public static float OutwardSortKey(ArHorizontalEdge edge, float behindBearingRadians) =>
        edge switch
        {
            ArHorizontalEdge.Left => -behindBearingRadians,
            ArHorizontalEdge.Right => behindBearingRadians,
            _ => 0f,
        };

    /// <summary>
    /// Write stacked X positions. <paramref name="sortKeys"/> / <paramref name="outXs"/> length = n.
    /// Highest sort key → outermost (<paramref name="outermostX"/>); then step inward by separation.
    /// Stable tie-break: lower index wins outward slot when keys are equal.
    /// </summary>
    public static void AssignStackedXs(
        ArHorizontalEdge edge,
        float outermostX,
        float separationPixels,
        float[] sortKeys,
        float[] outXs) =>
        AssignStackedXs(edge, outermostX, separationPixels, sortKeys, outXs, sortKeys?.Length ?? 0);

    /// <summary>
    /// Count-taking overload so per-frame callers can pass reusable buffers instead of exact-size
    /// arrays. Ranks in place (n is a handful of markers) — no order buffer, no comparison delegate,
    /// because this runs inside OnGUI on the GC-cadence path.
    /// </summary>
    public static void AssignStackedXs(
        ArHorizontalEdge edge,
        float outermostX,
        float separationPixels,
        float[] sortKeys,
        float[] outXs,
        int count)
    {
        if (sortKeys == null || outXs == null)
        {
            throw new ArgumentNullException(sortKeys == null ? nameof(sortKeys) : nameof(outXs));
        }

        if (count < 0 || count > sortKeys.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (outXs.Length < count)
        {
            throw new ArgumentException("outXs shorter than sortKeys.", nameof(outXs));
        }

        if (edge == ArHorizontalEdge.None || count == 0)
        {
            for (var i = 0; i < count; i++)
            {
                outXs[i] = outermostX;
            }

            return;
        }

        var inward = edge == ArHorizontalEdge.Left ? 1f : -1f;
        for (var i = 0; i < count; i++)
        {
            // Slot = how many markers sort ahead of this one (higher key first; lower index wins ties).
            var slot = 0;
            for (var j = 0; j < count; j++)
            {
                if (j == i)
                {
                    continue;
                }

                var cmp = sortKeys[j].CompareTo(sortKeys[i]);
                if (cmp > 0 || (cmp == 0 && j < i))
                {
                    slot++;
                }
            }

            outXs[i] = outermostX + slot * separationPixels * inward;
        }
    }
}
