using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Place Switch List AR / Arrived pin past the junction by consist length + margin
/// so Arrived · Next means clear of the switch gates (solo = small nudge).
/// </summary>
public static class SwitchListPinClearOffset
{
    /// <summary>
    /// Fixed DE2 clear distance past the frog (loco ~15 m + throw margin ~3 m).
    /// Not dynamic tip-span — avoids false CLEARED from tip/centroid projection.
    /// </summary>
    public const float De2ClearPastMeters = 18f;

    /// <summary>
    /// How far the cab must be past the frog along the corridor for CLEARED.
    /// Fixed DE2 baseline (consist length ignored).
    /// </summary>
    public static float ClearedPastMeters(float consistLengthMeters)
    {
        _ = consistLengthMeters;
        return De2ClearPastMeters;
    }

    /// <summary>
    /// Cab/player past the junction along corridor dir by ≥ pastMeters → Cleared.
    /// Reverser-independent (smoke: past=Fouling from travel axis while cab was past pin).
    /// </summary>
    public static ConsistClearanceStatus EvaluateCabPastAlong(
        float junctionX,
        float junctionZ,
        float cabX,
        float cabZ,
        float dirX,
        float dirZ,
        float pastMeters)
    {
        var len = (float)Math.Sqrt((dirX * dirX) + (dirZ * dirZ));
        if (len < 1e-4f || float.IsNaN(len) || pastMeters < 0f || float.IsNaN(pastMeters))
        {
            return ConsistClearanceStatus.Unknown;
        }

        var ux = dirX / len;
        var uz = dirZ / len;
        var proj = ((cabX - junctionX) * ux) + ((cabZ - junctionZ) * uz);
        return proj >= pastMeters
            ? ConsistClearanceStatus.Cleared
            : ConsistClearanceStatus.Fouling;
    }

    /// <summary>Meters past the frog to the clear-stop pin.</summary>
    public static float ClearOffsetMeters(
        float consistLengthMeters,
        float marginMeters = ConsistSwitchClearance.DefaultMarginMeters)
    {
        var len = consistLengthMeters > 0f && !float.IsNaN(consistLengthMeters)
            ? consistLengthMeters
            : 0f;
        var margin = marginMeters > 0f && !float.IsNaN(marginMeters)
            ? marginMeters
            : ConsistSwitchClearance.DefaultMarginMeters;
        return len + margin;
    }

    /// <summary>
    /// Offset junction XZ along corridor direction. Fail closed: returns junction
    /// coords and false when dir is zero/invalid or offset ≤ 0.
    /// </summary>
    public static bool TryOffsetPinXz(
        float jx,
        float jz,
        float dirX,
        float dirZ,
        float offsetMeters,
        out float pinX,
        out float pinZ)
    {
        pinX = jx;
        pinZ = jz;
        if (offsetMeters <= 0f || float.IsNaN(offsetMeters))
        {
            return false;
        }

        var len = (float)Math.Sqrt((dirX * dirX) + (dirZ * dirZ));
        if (len < 1e-4f || float.IsNaN(len))
        {
            return false;
        }

        var ux = dirX / len;
        var uz = dirZ / len;
        pinX = jx + (ux * offsetMeters);
        pinZ = jz + (uz * offsetMeters);
        return true;
    }

    /// <summary>
    /// Corridor direction past the pin junction: vector from the frog to the
    /// departure-track centroid of the junction hop (last hop when the junction
    /// is reused — Yard dual-branch / junction-first). Never track→track hops
    /// that can loop and invert the half-plane.
    /// </summary>
    public static bool TryCorridorDirAfterJunction(
        System.Collections.Generic.IReadOnlyList<string> trackIds,
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        string? junctionId,
        float jx,
        float jz,
        Func<string, float> worldX,
        Func<string, float> worldZ,
        out float dirX,
        out float dirZ)
    {
        dirX = dirZ = 0f;
        var jid = junctionId?.Trim();
        if (string.IsNullOrEmpty(jid)
            || trackIds == null
            || trackIds.Count < 2
            || edges == null
            || worldX == null
            || worldZ == null)
        {
            return false;
        }

        var jHop = -1;
        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var from = trackIds[i]?.Trim();
            var to = trackIds[i + 1]?.Trim();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                continue;
            }

            if (!TryFindJunctionHop(edges, from!, to!, jid!, out _))
            {
                continue;
            }

            // Last match wins — junction-first conflict is the later dual-branch use.
            jHop = i;
        }

        if (jHop < 0)
        {
            return false;
        }

        var toJ = trackIds[jHop + 1]?.Trim();
        if (string.IsNullOrEmpty(toJ))
        {
            return false;
        }

        var tx = worldX(toJ!);
        var tz = worldZ(toJ!);
        if (float.IsNaN(tx) || float.IsNaN(tz))
        {
            return false;
        }

        dirX = tx - jx;
        dirZ = tz - jz;
        return (dirX * dirX) + (dirZ * dirZ) > 1e-4f;
    }

    private static bool TryFindJunctionHop(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        string from,
        string to,
        string junctionId,
        out PathEdge hop)
    {
        hop = default;
        for (var i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (!e.HasJunction
                || e.JunctionId == null
                || !string.Equals(e.JunctionId, junctionId, StringComparison.Ordinal)
                || !string.Equals(e.FromTrackId, from, StringComparison.Ordinal)
                || !string.Equals(e.ToTrackId, to, StringComparison.Ordinal))
            {
                continue;
            }

            hop = e;
            return true;
        }

        return false;
    }
}
