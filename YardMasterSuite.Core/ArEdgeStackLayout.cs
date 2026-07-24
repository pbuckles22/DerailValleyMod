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
        float[] outXs)
    {
        if (sortKeys == null || outXs == null)
        {
            throw new ArgumentNullException(sortKeys == null ? nameof(sortKeys) : nameof(outXs));
        }

        var n = sortKeys.Length;
        if (outXs.Length < n)
        {
            throw new ArgumentException("outXs shorter than sortKeys.", nameof(outXs));
        }

        if (edge == ArHorizontalEdge.None || n == 0)
        {
            for (var i = 0; i < n; i++)
            {
                outXs[i] = outermostX;
            }

            return;
        }

        var inward = edge == ArHorizontalEdge.Left ? 1f : -1f;
        var order = new int[n];
        for (var i = 0; i < n; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (a, b) =>
        {
            var cmp = sortKeys[b].CompareTo(sortKeys[a]); // higher key first (outer)
            return cmp != 0 ? cmp : a.CompareTo(b);
        });

        for (var slot = 0; slot < n; slot++)
        {
            var index = order[slot];
            outXs[index] = outermostX + slot * separationPixels * inward;
        }
    }
}
