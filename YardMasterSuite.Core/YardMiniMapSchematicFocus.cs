using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Focus window for yard mini-map zoom (4.13 / 0.6.29 smoke FAIL).
/// Distant anonymous <c>#Y</c> mesh must not inflate AABB or named rails shrink to sub-pixel dashes.
/// </summary>
public static class YardMiniMapSchematicFocus
{
    /// <summary>
    /// OnGUI <c>DrawLine</c> drops chords shorter than this (panel pixels).
    /// </summary>
    public const float MinDrawableChordPixels = 0.5f;

    /// <summary>
    /// Include extra (#Y) sample points that lie within this distance of the named AABB
    /// (before padding) so paths to nearby TT stay in zoom without city-wide inflate.
    /// </summary>
    public const float DefaultExtraIncludeMeters = 150f;

    /// <summary>
    /// Build focus points for <see cref="YardMiniMapProjection.TryFitBounds"/>.
    /// Always includes <paramref name="namedPoints"/> and <paramref name="landmarks"/>.
    /// Includes <paramref name="extraPoints"/> only when inside named AABB expanded by
    /// <paramref name="extraIncludeMeters"/>.
    /// </summary>
    public static List<(float X, float Z)> CollectFocusPoints(
        IReadOnlyList<(float X, float Z)>? namedPoints,
        IReadOnlyList<(float X, float Z)>? extraPoints,
        IReadOnlyList<(float X, float Z)>? landmarks,
        float extraIncludeMeters = DefaultExtraIncludeMeters)
    {
        var result = new List<(float X, float Z)>(64);
        if (namedPoints != null)
        {
            for (var i = 0; i < namedPoints.Count; i++)
            {
                result.Add(namedPoints[i]);
            }
        }

        if (landmarks != null)
        {
            for (var i = 0; i < landmarks.Count; i++)
            {
                result.Add(landmarks[i]);
            }
        }

        if (extraPoints == null
            || extraPoints.Count == 0
            || namedPoints == null
            || namedPoints.Count == 0)
        {
            return result;
        }

        if (!YardMiniMapProjection.TryFitBounds(namedPoints, 0f, out var nMinX, out var nMaxX, out var nMinZ, out var nMaxZ))
        {
            return result;
        }

        var expand = extraIncludeMeters < 0f ? 0f : extraIncludeMeters;
        nMinX -= expand;
        nMaxX += expand;
        nMinZ -= expand;
        nMaxZ += expand;

        for (var i = 0; i < extraPoints.Count; i++)
        {
            var (x, z) = extraPoints[i];
            if (!YardMiniMapProjection.IsOutsideBounds(x, z, nMinX, nMaxX, nMinZ, nMaxZ))
            {
                result.Add((x, z));
            }
        }

        return result;
    }

    /// <summary>
    /// Panel-pixel length of a world chord after projection (0 when either end fails).
    /// </summary>
    public static float ProjectedChordPixels(
        float worldX0,
        float worldZ0,
        float worldX1,
        float worldZ1,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float panelWidth,
        float panelHeight)
    {
        if (!YardMiniMapProjection.TryWorldToPanel(
                worldX0, worldZ0, minX, maxX, minZ, maxZ, 0f, 0f, panelWidth, panelHeight,
                out var x0, out var y0))
        {
            return 0f;
        }

        if (!YardMiniMapProjection.TryWorldToPanel(
                worldX1, worldZ1, minX, maxX, minZ, maxZ, 0f, 0f, panelWidth, panelHeight,
                out var x1, out var y1))
        {
            return 0f;
        }

        var dx = x1 - x0;
        var dy = y1 - y0;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>True when a chord is long enough for the OnGUI line drawer.</summary>
    public static bool IsDrawableChord(float projectedPixels) =>
        projectedPixels >= MinDrawableChordPixels;
}
