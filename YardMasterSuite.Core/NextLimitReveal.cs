using System;

namespace YardMasterSuite.Core;

/// <summary>
/// When to show meters on the Next chip (**1.17**).
/// Far: <c>Next 40</c>. Close: <c>Next 40 (85m)</c>.
/// </summary>
public static class NextLimitReveal
{
    /// <summary>Comfortable soft decel used only for reveal distance (not Brake HUD).</summary>
    public const float ComfortDecelMps2 = 0.25f;

    public const float MinRevealMeters = 120f;
    public const float MaxRevealMeters = 600f;

    /// <summary>
    /// Along-track distance at which meters appear for a drop from <paramref name="fromKmh"/>
    /// to <paramref name="toKmh"/>. Same/higher Next uses a short cue window.
    /// Mass scales heavier consists a bit farther (capped).
    /// </summary>
    public static float RevealMeters(float fromKmh, float toKmh, float massTonnes = 40f)
    {
        if (!(fromKmh > 0f) || !IsFinite(fromKmh) || !IsFinite(toKmh))
        {
            return MinRevealMeters;
        }

        if (toKmh + 0.5f >= fromKmh)
        {
            return MinRevealMeters;
        }

        var v0 = fromKmh / 3.6f;
        var v1 = Math.Max(0f, toKmh) / 3.6f;
        var mass = massTonnes > 0f && IsFinite(massTonnes) ? massTonnes : 40f;
        // ~1.0 at 40 t, ~0.55 at 400 t — longer reveal for heavy.
        var a = ComfortDecelMps2 * (40f / Math.Max(40f, Math.Min(mass, 400f)));
        a = Math.Max(0.12f, a);
        var d = ((v0 * v0) - (v1 * v1)) / (2f * a);
        if (!IsFinite(d) || d < MinRevealMeters)
        {
            return MinRevealMeters;
        }

        return d > MaxRevealMeters ? MaxRevealMeters : d;
    }

    public static bool ShowDistance(
        float alongMeters,
        float fromKmh,
        float toKmh,
        float massTonnes = 40f) =>
        alongMeters > 0f && alongMeters <= RevealMeters(fromKmh, toKmh, massTonnes);

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
