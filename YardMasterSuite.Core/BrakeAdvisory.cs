using System;

namespace YardMasterSuite.Core;

public enum BrakeAdvisoryLevel
{
    /// <summary>Nothing slower ahead, or far enough that braking is not a decision yet.</summary>
    None,

    /// <summary>Slower board ahead and the soft window is open — start easing off.</summary>
    Advisory,

    /// <summary>Soft braking no longer fits: hard service braking is required now.</summary>
    Critical,

    /// <summary>Grade beats even hard braking — the consist cannot hold this restriction.</summary>
    Runaway,
}

public readonly struct BrakeAdvisoryState
{
    public static readonly BrakeAdvisoryState Silent =
        new(BrakeAdvisoryLevel.None, 0, 0, 0, string.Empty);

    public BrakeAdvisoryState(
        BrakeAdvisoryLevel level,
        int targetKmh,
        int distanceMeters,
        int etaSeconds,
        string text)
    {
        Level = level;
        TargetKmh = targetKmh;
        DistanceMeters = distanceMeters;
        EtaSeconds = etaSeconds;
        Text = text;
    }

    public BrakeAdvisoryLevel Level { get; }
    public int TargetKmh { get; }
    public int DistanceMeters { get; }

    /// <summary>Seconds to the board at current speed (0 when stopped / unknown).</summary>
    public int EtaSeconds { get; }

    public string Text { get; }
}

/// <summary>
/// Soft-brake advisory for the next posted board (**1.16**).
/// <para>
/// Two braking budgets: a <b>soft</b> one (comfortable easing — drives the yellow window and the
/// look-ahead Limit adopt) and a <b>hard</b> one (heavy service application — drives red). Both are
/// reduced by downhill gravity, so a descent warns much earlier than the same approach on the flat.
/// </para>
/// <para>
/// Grade math per A116: a −2.1 % grade adds ≈ 0.21 m/s² of acceleration, which alone exceeds the
/// soft budget of a loaded consist. When gravity beats the <i>hard</i> budget the state is
/// <see cref="BrakeAdvisoryLevel.Runaway"/> — brakes cannot hold, so say so instead of quoting a
/// braking distance the train cannot achieve.
/// </para>
/// </summary>
public static class BrakeAdvisory
{
    /// <summary>Soft service deceleration for a light loco (m/s²). Conservative on purpose.</summary>
    public const float MaxDecelMps2 = 0.18f;

    /// <summary>Soft service deceleration for a heavy loaded consist (m/s²).</summary>
    public const float MinDecelMps2 = 0.08f;

    /// <summary>Heavy service application, light loco (m/s²).</summary>
    public const float HardMaxDecelMps2 = 0.55f;

    /// <summary>Heavy service application, loaded consist (m/s²).</summary>
    public const float HardMinDecelMps2 = 0.30f;

    /// <summary>Net deceleration at or below this counts as "cannot slow down".</summary>
    public const float MinNetDecelMps2 = 0.03f;

    /// <summary>Mass at which we assume the heavy-consist deceleration (tonnes).</summary>
    public const float HeavyConsistTonnes = 600f;

    /// <summary>Mass below which we assume the light-loco deceleration (tonnes).</summary>
    public const float LightConsistTonnes = 60f;

    /// <summary>Thinking / throttle-idle time added to the soft brake time (seconds).</summary>
    public const float ReactionSeconds = 12f;

    /// <summary>Reaction allowance once hard braking is the plan (seconds).</summary>
    public const float HardReactionSeconds = 3f;

    /// <summary>Yellow advisory starts this many multiples of the soft required <b>time</b> out.</summary>
    public const float AdvisoryFactor = 3.5f;

    public const float GravityMps2 = 9.81f;

    /// <summary>Soft (comfort) deceleration between a light loco and a loaded consist.</summary>
    public static float DecelerationFor(float massTonnes) =>
        Interpolate(massTonnes, MaxDecelMps2, MinDecelMps2);

    /// <summary>Heavy service deceleration between a light loco and a loaded consist.</summary>
    public static float HardDecelerationFor(float massTonnes) =>
        Interpolate(massTonnes, HardMaxDecelMps2, HardMinDecelMps2);

    /// <summary>Along-track acceleration from gravity. Downhill (negative grade) is positive.</summary>
    public static float GradeAccelerationMps2(float gradePercent) =>
        GravityMps2 * (-gradePercent / 100f);

    /// <summary>Soft budget after gravity, floored so planning distances stay finite.</summary>
    public static float NetSoftDecelerationFor(float massTonnes, float gradePercent)
    {
        var net = DecelerationFor(massTonnes) - GradeAccelerationMps2(gradePercent);
        return net < MinNetDecelMps2 ? MinNetDecelMps2 : net;
    }

    /// <summary>Hard budget after gravity. May be zero or negative on a steep descent.</summary>
    public static float NetHardDecelerationFor(float massTonnes, float gradePercent) =>
        HardDecelerationFor(massTonnes) - GradeAccelerationMps2(gradePercent);

    /// <summary>True when even a heavy application cannot overcome the grade.</summary>
    public static bool IsRunaway(float massTonnes, float gradePercent) =>
        NetHardDecelerationFor(massTonnes, gradePercent) <= MinNetDecelMps2;

    /// <summary>Soft brake time (seconds) including reaction. Grade-aware.</summary>
    public static float RequiredTimeSeconds(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent = 0f)
    {
        if (!TryDelta(speedKmh, targetKmh, out var v, out var target))
        {
            return 0f;
        }

        return ((v - target) / NetSoftDecelerationFor(massTonnes, gradePercent)) + ReactionSeconds;
    }

    /// <summary>Soft distance needed to ease to <paramref name="targetKmh"/>. Grade-aware.</summary>
    public static float RequiredDistanceMeters(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent = 0f)
    {
        if (!TryDelta(speedKmh, targetKmh, out var v, out var target))
        {
            return 0f;
        }

        var decel = NetSoftDecelerationFor(massTonnes, gradePercent);
        return (((v * v) - (target * target)) / (2f * decel)) + (v * ReactionSeconds);
    }

    /// <summary>Hard-application time, or +∞ when the grade wins (runaway).</summary>
    public static float HardRequiredTimeSeconds(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent = 0f)
    {
        if (!TryDelta(speedKmh, targetKmh, out var v, out var target))
        {
            return 0f;
        }

        if (IsRunaway(massTonnes, gradePercent))
        {
            return float.PositiveInfinity;
        }

        var decel = NetHardDecelerationFor(massTonnes, gradePercent);
        return ((v - target) / decel) + HardReactionSeconds;
    }

    /// <summary>Seconds until the board at constant current speed (∞-ish when stopped).</summary>
    public static float TimeToBoardSeconds(float speedKmh, float distanceMeters)
    {
        var v = SpeedDisplay.ToMetersPerSecond(speedKmh);
        if (v < 0.5f || distanceMeters <= 0f)
        {
            return float.PositiveInfinity;
        }

        return distanceMeters / v;
    }

    /// <summary>
    /// Unknown mass is treated as a loaded consist: a late warning is worse than an early one.
    /// Yellow while the soft window is open, red once hard braking is required, and an explicit
    /// runaway state when the grade beats hard braking outright.
    /// </summary>
    public static BrakeAdvisoryState Evaluate(
        float? speedKmh,
        float? nextLimitKmh,
        float? nextDistanceMeters,
        float? massTonnes,
        float? gradePercent = null)
    {
        if (speedKmh is not float speed
            || nextLimitKmh is not float target
            || nextDistanceMeters is not float distance)
        {
            return BrakeAdvisoryState.Silent;
        }

        if (distance <= 0f || target >= speed)
        {
            return BrakeAdvisoryState.Silent;
        }

        var mass = massTonnes ?? HeavyConsistTonnes;
        var grade = gradePercent ?? 0f;
        var softTime = RequiredTimeSeconds(speed, target, mass, grade);
        var eta = TimeToBoardSeconds(speed, distance);
        if (softTime <= 0f || float.IsInfinity(eta))
        {
            return BrakeAdvisoryState.Silent;
        }

        var targetWhole = (int)Math.Round(target, MidpointRounding.AwayFromZero);
        var roundedMeters = RoundDistance(distance);
        var roundedEta = RoundEta(eta);

        if (IsRunaway(mass, grade))
        {
            return new BrakeAdvisoryState(
                BrakeAdvisoryLevel.Runaway,
                targetWhole,
                roundedMeters,
                roundedEta,
                $"RUNAWAY — Brake {targetWhole} NOW ({roundedMeters} m)");
        }

        if (eta > softTime * AdvisoryFactor)
        {
            return BrakeAdvisoryState.Silent;
        }

        var hardTime = HardRequiredTimeSeconds(speed, target, mass, grade);
        var level = eta <= hardTime ? BrakeAdvisoryLevel.Critical : BrakeAdvisoryLevel.Advisory;
        return new BrakeAdvisoryState(
            level,
            targetWhole,
            roundedMeters,
            roundedEta,
            $"Brake {targetWhole} in {roundedEta} s ({roundedMeters} m)");
    }

    /// <summary>
    /// Rich-text chip: yellow while there is still room, red once hard braking is needed.
    /// Empty when silent, so the chip only exists when there is something to do.
    /// </summary>
    public static string FormatHud(BrakeAdvisoryState state)
    {
        if (state.Level == BrakeAdvisoryLevel.None || string.IsNullOrEmpty(state.Text))
        {
            return string.Empty;
        }

        if (state.Level == BrakeAdvisoryLevel.Runaway)
        {
            return $"<b><color={SpeedLimitDisplay.CriticalColor}>{state.Text}</color></b>";
        }

        var color = state.Level == BrakeAdvisoryLevel.Critical
            ? SpeedLimitDisplay.CriticalColor
            : SpeedLimitDisplay.WarningColor;
        return $"<color={color}>{state.Text}</color>";
    }

    private static float Interpolate(float massTonnes, float light, float heavy)
    {
        if (massTonnes <= LightConsistTonnes)
        {
            return light;
        }

        if (massTonnes >= HeavyConsistTonnes)
        {
            return heavy;
        }

        var t = (massTonnes - LightConsistTonnes) / (HeavyConsistTonnes - LightConsistTonnes);
        return light + ((heavy - light) * t);
    }

    private static bool TryDelta(float speedKmh, float targetKmh, out float v, out float target)
    {
        v = SpeedDisplay.ToMetersPerSecond(speedKmh);
        target = SpeedDisplay.ToMetersPerSecond(targetKmh);
        return target < v;
    }

    private static int RoundDistance(float meters)
    {
        if (meters < 100f)
        {
            return (int)(Math.Round(meters / 5f, MidpointRounding.AwayFromZero) * 5);
        }

        return (int)(Math.Round(meters / 10f, MidpointRounding.AwayFromZero) * 10);
    }

    private static int RoundEta(float seconds)
    {
        if (seconds < 30f)
        {
            return (int)Math.Max(1, Math.Round(seconds, MidpointRounding.AwayFromZero));
        }

        return (int)(Math.Round(seconds / 5f, MidpointRounding.AwayFromZero) * 5);
    }
}
