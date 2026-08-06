using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure XZ → panel projection for the yard mini-map (4.13).
/// Panel Y grows downward (OnGUI); world +Z (north) maps toward the top of the panel.
/// Heading 0° = north (+Z), clockwise toward +X — same as <see cref="HeadingDisplay"/>.
/// </summary>
public static class YardMiniMapProjection
{
    private const float MinSpan = 1e-3f;
    private const double Deg2Rad = Math.PI / 180.0;

    /// <summary>
    /// Axis-aligned bounds from world XZ samples, expanded by <paramref name="padding"/> on each side.
    /// </summary>
    public static bool TryFitBounds(
        IReadOnlyList<(float X, float Z)> points,
        float padding,
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        minX = maxX = minZ = maxZ = 0f;
        if (points == null || points.Count == 0)
        {
            return false;
        }

        minX = maxX = points[0].X;
        minZ = maxZ = points[0].Z;
        for (var i = 1; i < points.Count; i++)
        {
            var (x, z) = points[i];
            if (x < minX)
            {
                minX = x;
            }

            if (x > maxX)
            {
                maxX = x;
            }

            if (z < minZ)
            {
                minZ = z;
            }

            if (z > maxZ)
            {
                maxZ = z;
            }
        }

        var pad = padding < 0f ? 0f : padding;
        minX -= pad;
        maxX += pad;
        minZ -= pad;
        maxZ += pad;
        return true;
    }

    /// <summary>
    /// Map world XZ into a panel rect. Fails when bounds span is degenerate.
    /// </summary>
    public static bool TryWorldToPanel(
        float worldX,
        float worldZ,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float panelLeft,
        float panelTop,
        float panelWidth,
        float panelHeight,
        out float panelX,
        out float panelY)
    {
        panelX = panelY = 0f;
        var spanX = maxX - minX;
        var spanZ = maxZ - minZ;
        if (spanX < MinSpan || spanZ < MinSpan || panelWidth <= 0f || panelHeight <= 0f)
        {
            return false;
        }

        var u = (worldX - minX) / spanX;
        var v = (worldZ - minZ) / spanZ; // 0 = south, 1 = north
        panelX = panelLeft + u * panelWidth;
        panelY = panelTop + (1f - v) * panelHeight;
        return true;
    }

    /// <summary>
    /// Pixel offset from pin toward heading. North (0°) → up on panel (negative OnGUI Y).
    /// </summary>
    public static void HeadingTickOffset(float headingDegrees, float lengthPixels, out float dx, out float dy)
    {
        var rad = headingDegrees * Deg2Rad;
        dx = (float)(Math.Sin(rad) * lengthPixels);
        dy = (float)(-Math.Cos(rad) * lengthPixels);
    }

    /// <summary>True when world XZ is outside the schematic AABB.</summary>
    public static bool IsOutsideBounds(
        float worldX,
        float worldZ,
        float minX,
        float maxX,
        float minZ,
        float maxZ) =>
        worldX < minX || worldX > maxX || worldZ < minZ || worldZ > maxZ;

    /// <summary>
    /// When the player is outside the schematic, place an edge cue on the panel border
    /// pointing toward them. Returns false when inside (use the normal pin instead).
    /// </summary>
    public static bool TryOffMapEdge(
        float worldX,
        float worldZ,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        float panelLeft,
        float panelTop,
        float panelWidth,
        float panelHeight,
        float insetPixels,
        out float edgeX,
        out float edgeY,
        out float dirX,
        out float dirY)
    {
        edgeX = edgeY = dirX = dirY = 0f;
        if (!IsOutsideBounds(worldX, worldZ, minX, maxX, minZ, maxZ))
        {
            return false;
        }

        if (!TryWorldToPanel(
                worldX,
                worldZ,
                minX,
                maxX,
                minZ,
                maxZ,
                panelLeft,
                panelTop,
                panelWidth,
                panelHeight,
                out edgeX,
                out edgeY))
        {
            return false;
        }

        ClampToPanel(panelLeft, panelTop, panelWidth, panelHeight, insetPixels, ref edgeX, ref edgeY);

        var cx = panelLeft + panelWidth * 0.5f;
        var cy = panelTop + panelHeight * 0.5f;
        dirX = edgeX - cx;
        dirY = edgeY - cy;
        var len = (float)Math.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 1e-3f)
        {
            dirX = 0f;
            dirY = -1f;
        }
        else
        {
            dirX /= len;
            dirY /= len;
        }

        return true;
    }

    /// <summary>
    /// Keep a projected point inside the panel (with optional inset).
    /// </summary>
    public static void ClampToPanel(
        float panelLeft,
        float panelTop,
        float panelWidth,
        float panelHeight,
        float insetPixels,
        ref float panelX,
        ref float panelY)
    {
        var inset = insetPixels < 0f ? 0f : insetPixels;
        var minX = panelLeft + inset;
        var maxX = panelLeft + panelWidth - inset;
        var minY = panelTop + inset;
        var maxY = panelTop + panelHeight - inset;
        if (maxX < minX)
        {
            minX = maxX = panelLeft + panelWidth * 0.5f;
        }

        if (maxY < minY)
        {
            minY = maxY = panelTop + panelHeight * 0.5f;
        }

        if (panelX < minX)
        {
            panelX = minX;
        }
        else if (panelX > maxX)
        {
            panelX = maxX;
        }

        if (panelY < minY)
        {
            panelY = minY;
        }
        else if (panelY > maxY)
        {
            panelY = maxY;
        }
    }
}
