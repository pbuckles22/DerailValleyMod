using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Resolve a tight lobby/office footprint for AR house-icon hide (A.4).
/// Prefers architectural BoxCollider near JobValidator, then mesh bounds;
/// rejects yard-sized boxes. Prefer hide just inside the door over apron false-positives.
/// </summary>
internal static class StationOfficeBounds
{
    /// <summary>Pull footprint inward so sidewalk / door stay outside the hide volume.</summary>
    public const float ShrinkExpandXZ = -2f;

    public const float MaxValidatorDistanceMeters = 80f;
    public const float MinBuildingSizeXZ = 6f;
    /// <summary>Reject platform/yard colliders larger than a station office shell.</summary>
    public const float MaxBuildingSizeXZ = 25f;

    /// <summary>
    /// Fallback half-extent from deep office anchor: keep SM door (~14 m) outside the box.
    /// </summary>
    public const float FallbackHalfExtentXZ = 8f;
    public const float FallbackHalfExtentY = 6f;

    private static string? _cachedYardId;
    private static Aabb3 _cachedAabb;
    private static bool _hasCache;

    public static void ClearCache()
    {
        _hasCache = false;
        _cachedYardId = null;
    }

    public static bool TryGetHideAabb(StationController station, Vector3 officeAnchor, out Aabb3 aabb)
    {
        aabb = default;
        if (station == null)
        {
            return false;
        }

        var yardId = station.stationInfo != null ? station.stationInfo.YardID : station.name;
        if (!OfficeBoundsCachePolicy.ShouldResolve(_cachedYardId, yardId, _hasCache))
        {
            if (!_hasCache)
            {
                return false;
            }

            aabb = _cachedAabb;
            return true;
        }

        if (!TryResolve(officeAnchor, out aabb))
        {
            return false;
        }

        _cachedAabb = aabb;
        _cachedYardId = yardId;
        _hasCache = true;
        return true;
    }

    private static bool TryResolve(Vector3 officeAnchor, out Aabb3 aabb)
    {
        aabb = default;

        if (TryFromJobValidator(officeAnchor, out aabb))
        {
            return true;
        }

        aabb = Aabb3.FromCenterExtents(
            officeAnchor.x,
            officeAnchor.y + 1f,
            officeAnchor.z,
            FallbackHalfExtentXZ,
            FallbackHalfExtentY,
            FallbackHalfExtentXZ);
        aabb = aabb.InflateXZ(ShrinkExpandXZ);
        return true;
    }

    private static bool TryFromJobValidator(Vector3 officeAnchor, out Aabb3 aabb)
    {
        aabb = default;
        JobValidator? best = null;
        var bestDist = float.MaxValue;
        var maxSqr = MaxValidatorDistanceMeters * MaxValidatorDistanceMeters;

        JobValidator[] validators;
        try
        {
            validators = Object.FindObjectsOfType<JobValidator>();
        }
        catch
        {
            return false;
        }

        if (validators == null || validators.Length == 0)
        {
            return false;
        }

        foreach (var v in validators)
        {
            if (v == null)
            {
                continue;
            }

            var d = (v.transform.position - officeAnchor).sqrMagnitude;
            if (d < bestDist && d <= maxSqr)
            {
                bestDist = d;
                best = v;
            }
        }

        if (best == null)
        {
            return false;
        }

        if (TryLargestBuildingBox(best.transform, out aabb))
        {
            aabb = aabb.InflateXZ(ShrinkExpandXZ);
            return IsPlausibleBuilding(aabb);
        }

        if (TryEncapsulateRenderers(best.transform, out aabb))
        {
            aabb = aabb.InflateXZ(ShrinkExpandXZ);
            return IsPlausibleBuilding(aabb);
        }

        // Validator found but no mesh/collider — lobby-sized estimate around it.
        var p = best.transform.position;
        aabb = Aabb3.FromCenterExtents(
            p.x, p.y + 1f, p.z, FallbackHalfExtentXZ, FallbackHalfExtentY, FallbackHalfExtentXZ);
        aabb = aabb.InflateXZ(ShrinkExpandXZ);
        return true;
    }

    private static bool TryLargestBuildingBox(Transform start, out Aabb3 aabb)
    {
        aabb = default;
        var bestArea = 0f;
        var found = false;
        Aabb3 best = default;

        var t = start;
        for (var depth = 0; depth < 8 && t != null; depth++)
        {
            try
            {
                foreach (var box in t.GetComponentsInChildren<BoxCollider>(true))
                {
                    if (box == null || !box.enabled)
                    {
                        continue;
                    }

                    if ((box.bounds.center - start.position).sqrMagnitude > 35f * 35f)
                    {
                        continue;
                    }

                    var candidate = ToAabb(box.bounds);
                    if (!IsPlausibleBuilding(candidate))
                    {
                        continue;
                    }

                    var area = candidate.SizeX * candidate.SizeZ;
                    // Prefer larger footprint (whole building over furniture triggers).
                    var score = area + (box.isTrigger ? area * 0.05f : 0f);
                    if (score > bestArea)
                    {
                        bestArea = score;
                        best = candidate;
                        found = true;
                    }
                }
            }
            catch
            {
                // ignore
            }

            t = t.parent;
        }

        if (!found)
        {
            return false;
        }

        aabb = best;
        return true;
    }

    private static bool TryEncapsulateRenderers(Transform start, out Aabb3 aabb)
    {
        aabb = default;

        // Climb to a likely building root, then encapsulate nearby meshes.
        var root = start;
        for (var i = 0; i < 3 && root.parent != null; i++)
        {
            root = root.parent;
        }

        Renderer[] renderers;
        try
        {
            renderers = root.GetComponentsInChildren<Renderer>(true);
        }
        catch
        {
            return false;
        }

        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        var has = false;
        var enc = new Bounds(start.position, Vector3.one);
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled)
            {
                continue;
            }

            if ((r.bounds.center - start.position).sqrMagnitude > 40f * 40f)
            {
                continue;
            }

            // Skip tiny props.
            var s = r.bounds.size;
            if (s.x * s.z < 1f)
            {
                continue;
            }

            if (!has)
            {
                enc = r.bounds;
                has = true;
            }
            else
            {
                enc.Encapsulate(r.bounds);
            }
        }

        if (!has)
        {
            return false;
        }

        aabb = ToAabb(enc);
        return IsPlausibleBuilding(aabb);
    }

    private static Aabb3 ToAabb(Bounds b) =>
        new(b.min.x, b.min.y, b.min.z, b.max.x, b.max.y, b.max.z);

    private static bool IsPlausibleBuilding(in Aabb3 a)
    {
        var sx = a.SizeX;
        var sz = a.SizeZ;
        return sx >= MinBuildingSizeXZ && sz >= MinBuildingSizeXZ
            && sx <= MaxBuildingSizeXZ && sz <= MaxBuildingSizeXZ;
    }
}
