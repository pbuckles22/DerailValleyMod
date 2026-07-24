namespace YardMasterSuite.Core;

/// <summary>
/// Office / loco proximity for AR hide and Station <c>here</c> (Bundle A.4 + C).
/// Prefer an exact building AABB when available; otherwise a flat XZ radius.
/// </summary>
public static class ArProximityHide
{
    /// <summary>Flat XZ radius when no building box is available.</summary>
    public const float OfficeHideRadiusMeters = 20f;

    /// <summary>True when the player is on/in the AR loco (no self-marker).</summary>
    public static bool ShouldHideLocoMarker(bool playerCarIsTargetLoco) => playerCarIsTargetLoco;

    /// <summary>
    /// Same gate for house AR hide and Station chip <c>here</c> (exact footprint).
    /// Y ignored — floors/ceilings vary.
    /// </summary>
    public static bool IsAtOffice(in Aabb3 officeBounds, float playerX, float playerZ) =>
        officeBounds.ContainsXZ(playerX, playerZ);

    /// <summary>Flat-radius fallback when no exact building box exists.</summary>
    public static bool IsAtOffice(
        float officeX,
        float officeZ,
        float playerX,
        float playerZ,
        float radiusMeters = OfficeHideRadiusMeters)
    {
        var dx = officeX - playerX;
        var dz = officeZ - playerZ;
        var r = radiusMeters;
        return (dx * dx) + (dz * dz) <= r * r;
    }

    /// <summary>AR house-icon hide — identical to <see cref="IsAtOffice(in Aabb3, float, float)"/>.</summary>
    public static bool ShouldHideStationMarker(in Aabb3 officeBounds, float playerX, float playerZ) =>
        IsAtOffice(officeBounds, playerX, playerZ);
}

/// <summary>Axis-aligned box in world space (pure; no Unity dependency).</summary>
public readonly struct Aabb3
{
    public readonly float MinX;
    public readonly float MinY;
    public readonly float MinZ;
    public readonly float MaxX;
    public readonly float MaxY;
    public readonly float MaxZ;

    public Aabb3(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        MinX = minX;
        MinY = minY;
        MinZ = minZ;
        MaxX = maxX;
        MaxY = maxY;
        MaxZ = maxZ;
    }

    public float SizeX => MaxX - MinX;
    public float SizeY => MaxY - MinY;
    public float SizeZ => MaxZ - MinZ;

    public bool Contains(float x, float y, float z) =>
        x >= MinX && x <= MaxX
        && y >= MinY && y <= MaxY
        && z >= MinZ && z <= MaxZ;

    public bool ContainsXZ(float x, float z) =>
        x >= MinX && x <= MaxX
        && z >= MinZ && z <= MaxZ;

    /// <summary>
    /// Expand (positive) or shrink (negative) each axis size by <paramref name="delta"/> total
    /// (half on each side), matching Unity <c>Bounds.Expand</c> semantics.
    /// </summary>
    public Aabb3 Inflate(float delta)
    {
        var hx = delta * 0.5f;
        var hy = delta * 0.5f;
        var hz = delta * 0.5f;
        return ClampAxes(MinX - hx, MinY - hy, MinZ - hz, MaxX + hx, MaxY + hy, MaxZ + hz);
    }

    /// <summary>Inflate only XZ (building footprint); leave Y unchanged.</summary>
    public Aabb3 InflateXZ(float delta)
    {
        var hx = delta * 0.5f;
        var hz = delta * 0.5f;
        return ClampAxes(MinX - hx, MinY, MinZ - hz, MaxX + hx, MaxY, MaxZ + hz);
    }

    public static Aabb3 FromCenterExtents(float cx, float cy, float cz, float ex, float ey, float ez) =>
        new(cx - ex, cy - ey, cz - ez, cx + ex, cy + ey, cz + ez);

    private static Aabb3 ClampAxes(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        if (minX > maxX)
        {
            var m = (minX + maxX) * 0.5f;
            minX = maxX = m;
        }

        if (minY > maxY)
        {
            var m = (minY + maxY) * 0.5f;
            minY = maxY = m;
        }

        if (minZ > maxZ)
        {
            var m = (minZ + maxZ) * 0.5f;
            minZ = maxZ = m;
        }

        return new Aabb3(minX, minY, minZ, maxX, maxY, maxZ);
    }
}
