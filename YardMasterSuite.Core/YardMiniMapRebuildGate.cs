using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Mini-map snapshot rebuild gate (4.13). Prevents OnGUI thrash rebuilding hundreds of polylines.
/// </summary>
public static class YardMiniMapRebuildGate
{
    public const float DefaultRebuildIntervalSeconds = 2.5f;

    /// <summary>
    /// True when a new build should run. Yard change always rebuilds; otherwise wait until
    /// <paramref name="nowSeconds"/> &gt;= <paramref name="nextRebuildAt"/>.
    /// </summary>
    public static bool ShouldRebuild(
        float nowSeconds,
        float nextRebuildAt,
        string? cachedYardId,
        string? requestedYardId)
    {
        var requested = requestedYardId?.Trim();
        if (string.IsNullOrEmpty(requested))
        {
            return false;
        }

        var cached = cachedYardId?.Trim();
        if (!string.Equals(cached, requested, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return nowSeconds >= nextRebuildAt;
    }

    /// <summary>True when a world polyline has any vertex inside (or on) the AABB.</summary>
    public static bool PolylineIntersectsBounds(
        (float X, float Z)[]? poly,
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        if (poly == null || poly.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < poly.Length; i++)
        {
            var (x, z) = poly[i];
            if (!YardMiniMapProjection.IsOutsideBounds(x, z, minX, maxX, minZ, maxZ))
            {
                return true;
            }
        }

        return false;
    }
}
