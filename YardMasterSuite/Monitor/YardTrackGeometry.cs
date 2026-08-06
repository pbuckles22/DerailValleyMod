using System;
using System.Collections.Generic;
using UnityEngine;

namespace YardMasterSuite.Monitor;

/// <summary>Sample rail Bezier curves to world XZ polylines for the yard mini-map (4.13).</summary>
internal static class YardTrackGeometry
{
    /// <summary>Approximate sample spacing along the curve (meters).</summary>
    public const float DefaultSampleSpacingMeters = 20f;

    public static bool TrySampleTrackXZ(
        RailTrack? rail,
        List<(float X, float Z)> into,
        float sampleSpacingMeters = DefaultSampleSpacingMeters)
    {
        if (rail == null || into == null)
        {
            return false;
        }

        try
        {
            var curve = rail.curve;
            if (curve == null || curve.pointCount < 2)
            {
                var p = rail.transform.position;
                into.Add((p.x, p.z));
                return true;
            }

            var length = curve.length;
            if (length < 1f)
            {
                length = 1f;
            }

            var spacing = sampleSpacingMeters < 5f ? 5f : sampleSpacingMeters;
            var steps = Math.Max(1, (int)Math.Ceiling(length / spacing));
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var p = curve.GetPointAt(t);
                into.Add((p.x, p.z));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
