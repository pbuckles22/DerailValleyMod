namespace YardMasterSuite.Core;

/// <summary>
/// When to omit AR self-markers (Bundle A.4).
/// Station hide uses an AABB footprint (XZ), not a sphere.
/// </summary>
public static class ArProximityHide
{
    /// <summary>
    /// Legacy flat-radius constant (pre–bounds). Kept for docs; hide path uses <see cref="Aabb3"/>.
    /// </summary>
    public const float OfficeHideRadiusMeters = 20f;

    /// <summary>True when the player is on/in the AR loco (no self-marker).</summary>
    public static bool ShouldHideLocoMarker(bool playerCarIsTargetLoco) => playerCarIsTargetLoco;

    /// <summary>
    /// True when player XZ is inside the office footprint (Y ignored — floors/ceilings vary).
    /// </summary>
    public static bool ShouldHideStationMarker(in Aabb3 officeBounds, float playerX, float playerZ) =>
        officeBounds.ContainsXZ(playerX, playerZ);
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
