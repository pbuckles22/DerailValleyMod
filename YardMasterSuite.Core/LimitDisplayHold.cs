using System;

namespace YardMasterSuite.Core;

/// <summary>Where the Limit chip number came from (internal hold only — not shown on HUD).</summary>
public enum LimitAuthority
{
    None,

    /// <summary>Sticky / governing posted board.</summary>
    Posted,

    /// <summary>Unused after 1.17 — kept so hold state deserializes cleanly.</summary>
    Geometry,
}

/// <summary>
/// Stops Limit from flashing between posted candidates.
/// Tighter (lower) numbers apply immediately; looser (higher) numbers wait
/// <see cref="LoosenHoldSeconds"/>.
/// At standstill, the held number is frozen so travel/facing jitter cannot 40↔80.
/// </summary>
public static class LimitDisplayHold
{
    public const float LoosenHoldSeconds = 5f;
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
            return new State(candidate, candidateAuthority);
        }

        if (c > h)
        {
            return heldAgeSeconds >= LoosenHoldSeconds
                ? new State(candidate, candidateAuthority)
                : new State(held, heldAuthority);
        }

        if (candidateAuthority == LimitAuthority.None || candidateAuthority == heldAuthority)
        {
            return new State(held, heldAuthority);
        }

        if (heldAuthority == LimitAuthority.Geometry
            && candidateAuthority == LimitAuthority.Posted)
        {
            return new State(held, LimitAuthority.Posted);
        }

        if (heldAuthority == LimitAuthority.Posted
            && candidateAuthority == LimitAuthority.Geometry)
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
