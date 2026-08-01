using System;

namespace YardMasterSuite.Core;

/// <summary>Where the Limit chip number came from.</summary>
public enum LimitAuthority
{
    None,

    /// <summary>Sticky / governing posted board (sign you already own).</summary>
    Posted,

    /// <summary>Look-ahead adopt — calculated from boards ahead + soft-brake lead.</summary>
    Recommended,

    /// <summary>Geometry gap-fill when no posted board is known.</summary>
    Geometry,
}

/// <summary>
/// Stops Limit from flashing between look-ahead candidates.
/// Tighter (lower) numbers apply immediately; looser (higher) numbers wait
/// <see cref="LoosenHoldSeconds"/> so 50↔60 churn does not thrash the HUD.
/// At standstill, the held number is frozen so travel/facing jitter cannot 40↔80.
/// </summary>
public static class LimitDisplayHold
{
    /// <summary>How long a stricter Limit must stay before a looser one can replace it.</summary>
    public const float LoosenHoldSeconds = 5f;

    /// <summary>At or below this speed, Limit number/authority stays frozen (facing jitter).</summary>
    public const float StandstillMaxSpeedKmh = 0.5f;

    public readonly struct State
    {
        public State(float? limitKmh, LimitAuthority authority)
        {
            LimitKmh = limitKmh;
            Authority = authority;
        }

        public float? LimitKmh { get; }
        public LimitAuthority Authority { get; }
    }

    /// <summary>
    /// <paramref name="heldAgeSeconds"/> = time since the held number last <b>changed</b>
    /// (not since last frame).
    /// <paramref name="speedKmh"/> when ≤ <see cref="StandstillMaxSpeedKmh"/> freezes the held Limit
    /// so dual-facing / travel sign flicker at Speed 0 cannot thrash the chip.
    /// </summary>
    public static State Step(
        float? candidateKmh,
        LimitAuthority candidateAuthority,
        float? heldKmh,
        LimitAuthority heldAuthority,
        float heldAgeSeconds,
        float speedKmh = float.MaxValue)
    {
        if (heldKmh is not null
            && speedKmh <= StandstillMaxSpeedKmh
            && candidateKmh is not null)
        {
            return new State(heldKmh, heldAuthority);
        }

        if (candidateKmh is not float candidate)
        {
            if (heldKmh is null)
            {
                return new State(null, LimitAuthority.None);
            }

            // Lost all authority — clear after the same hold so a blink of no-signs does not wipe.
            return heldAgeSeconds >= LoosenHoldSeconds
                ? new State(null, LimitAuthority.None)
                : new State(heldKmh, heldAuthority);
        }

        if (heldKmh is not float held)
        {
            return new State(candidate, candidateAuthority);
        }

        var c = Round(candidate);
        var h = Round(held);
        if (c < h)
        {
            // Stricter — apply immediately (derailment safety).
            return new State(candidate, candidateAuthority);
        }

        if (c > h)
        {
            // Looser — wait out the hold.
            return heldAgeSeconds >= LoosenHoldSeconds
                ? new State(candidate, candidateAuthority)
                : new State(held, heldAuthority);
        }

        // Same number — authority UX:
        // Recommended → Posted when the board is taken (immediate).
        // Posted must not flip back to Recommended for the same km/h (label flash).
        if (candidateAuthority == LimitAuthority.None || candidateAuthority == heldAuthority)
        {
            return new State(held, heldAuthority);
        }

        if (heldAuthority == LimitAuthority.Recommended
            && candidateAuthority == LimitAuthority.Posted)
        {
            return new State(held, LimitAuthority.Posted);
        }

        if (heldAuthority == LimitAuthority.Posted
            && candidateAuthority == LimitAuthority.Recommended)
        {
            return new State(held, LimitAuthority.Posted);
        }

        return heldAgeSeconds >= LoosenHoldSeconds
            ? new State(candidate, candidateAuthority)
            : new State(held, heldAuthority);
    }

    public static bool NumberChanged(float? previous, float? next)
    {
        if (previous is null && next is null)
        {
            return false;
        }

        if (previous is null || next is null)
        {
            return true;
        }

        return Round(previous.Value) != Round(next.Value);
    }

    private static int Round(float kmh) =>
        (int)Math.Round(kmh, MidpointRounding.AwayFromZero);
}
