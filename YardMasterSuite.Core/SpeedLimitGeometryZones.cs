using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Converts raw curve-arc samples into a governing geometry limit without phantom dips.
/// Micro-kinks on procedural splines are shorter than <see cref="MinZoneLengthMeters"/> and are ignored.
/// </summary>
public static class SpeedLimitGeometryZones
{
    /// <summary>A speed zone must span at least this many meters of arc to count.</summary>
    public const float MinZoneLengthMeters = 15f;

    public readonly struct ArcSample
    {
        public ArcSample(float radiusMeters, float lengthMeters)
        {
            RadiusMeters = radiusMeters;
            LengthMeters = lengthMeters;
        }

        public float RadiusMeters { get; }
        public float LengthMeters { get; }
    }

    /// <summary>
    /// Lowest SignPlacer-ladder limit among sustained zones (merged consecutive equal limits).
    /// Returns null when there are no usable samples; caller may treat that as unrestricted.
    /// </summary>
    public static float? GoverningLimitKmh(IReadOnlyList<ArcSample> arcs, float minZoneLengthMeters = MinZoneLengthMeters)
    {
        if (arcs == null || arcs.Count == 0)
        {
            return null;
        }

        float? zoneLimit = null;
        var zoneLength = 0f;
        float? best = null;

        for (var i = 0; i < arcs.Count; i++)
        {
            var arc = arcs[i];
            if (arc.LengthMeters <= 0f || arc.RadiusMeters <= 0f
                || float.IsNaN(arc.RadiusMeters) || float.IsInfinity(arc.RadiusMeters))
            {
                continue;
            }

            var limit = SpeedLimitGeometry.MaxSpeedForMinRadius(arc.RadiusMeters);
            if (limit is null)
            {
                continue;
            }

            if (zoneLimit is float zl && Math.Abs(zl - limit.Value) < 0.5f)
            {
                zoneLength += arc.LengthMeters;
            }
            else
            {
                Consider(ref best, zoneLimit, zoneLength, minZoneLengthMeters);
                zoneLimit = limit;
                zoneLength = arc.LengthMeters;
            }
        }

        Consider(ref best, zoneLimit, zoneLength, minZoneLengthMeters);
        return best;
    }

    private static void Consider(ref float? best, float? zoneLimit, float zoneLength, float minZoneLength)
    {
        if (zoneLimit is not float limit || zoneLength < minZoneLength)
        {
            return;
        }

        best = best is float b ? Math.Min(b, limit) : limit;
    }
}
