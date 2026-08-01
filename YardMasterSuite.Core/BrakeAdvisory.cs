using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Heavy service application, light loco (m/s²).
    /// Retuned from 0.55 using the 0.5.57 DE2 trace: 69→49 km/h over ~470 m at full brakes.
    /// </summary>
    public const float HardMaxDecelMps2 = 0.25f;

    /// <summary>Heavy service application, loaded consist (m/s²), conservatively retuned.</summary>
    public const float HardMinDecelMps2 = 0.15f;

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

    /// <summary>
    /// Severe far-board warning opens at estimated slowdown time plus planning margin.
    /// Was 50% through 0.5.64, 25% through 0.5.67; corpus 067h showed the train paced far
    /// below posted — tighten to <b>10%</b> so Brake opens later (0.5.68).
    /// </summary>
    public const float WarningTimeMarginFactor = 1.10f;

    /// <summary>
    /// A target already on screen keeps its window this much wider.
    /// Without it, grade wobble at the window edge blinked the chip on and off between frames
    /// (0.5.59: <c>adv=Advisory 50</c> ↔ <c>adv=None</c> while Limit sat at 60).
    /// </summary>
    public const float WarningLatchReleaseFactor = 1.35f;

    /// <summary>
    /// Only warn about a board we must actually lose speed for. Matches the Limit chip's own
    /// yellow tolerance (<see cref="SpeedLimitDisplay.NearAboveKmh"/>): if the Limit chip is not
    /// showing red for this number, Brake should not either — no reason to flag "Brake 40 in Xs"
    /// while cruising at 44 in the yellow band of a 40 (A116 deep-dive, issue #2).
    /// </summary>
    public const float MinTargetDeltaKmh = SpeedLimitDisplay.NearAboveKmh;

    /// <summary>Lowest severe restriction used to size the route scan before boards are known.</summary>
    public const float WarningLookaheadTargetKmh = 30f;

    /// <summary>
    /// Bound route scan / Brake window. Was 6 km; corpus often warned for 30 at 3–3.8 km with
    /// no matching take — cap at 4.5 km so phantom far boards nag less (0.5.68).
    /// </summary>
    public const float MaxWarningLookaheadMeters = 4500f;

    /// <summary>
    /// Uncalibrated locomotive types use a slower, safety-biased planning profile.
    /// DE2 is calibrated from the 0.5.57 live trace.
    /// </summary>
    public const float UncalibratedLocoDecelerationFactor = 0.8f;

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

    /// <summary>
    /// Estimated comfortable slowdown time for route planning, including reaction.
    /// Uses consist mass, grade, and a locomotive-type calibration/fallback.
    /// </summary>
    public static float EstimatedSlowdownTimeSeconds(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent,
        string? locomotiveTypeId)
    {
        if (!TryDelta(speedKmh, targetKmh, out var speed, out var target))
        {
            return 0f;
        }

        var net = (DecelerationFor(massTonnes) * TypeFactor(locomotiveTypeId))
                  - GradeAccelerationMps2(gradePercent);
        var planningDecel = net < MinNetDecelMps2 ? MinNetDecelMps2 : net;
        return ((speed - target) / planningDecel) + ReactionSeconds;
    }

    /// <summary>
    /// Room a heavy application actually needs to reach <paramref name="targetKmh"/>, with the
    /// net deceleration floored so a descent that beats the brakes still quotes a finite number
    /// instead of going quiet.
    /// </summary>
    public static float GuaranteedStopDistanceMeters(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent,
        string? locomotiveTypeId)
    {
        if (!TryDelta(speedKmh, targetKmh, out var v, out var target))
        {
            return 0f;
        }

        var net = (HardDecelerationFor(massTonnes) * TypeFactor(locomotiveTypeId))
                  - GradeAccelerationMps2(gradePercent);
        var decel = net < MinNetDecelMps2 ? MinNetDecelMps2 : net;
        return (((v * v) - (target * target)) / (2f * decel)) + (v * HardReactionSeconds);
    }

    /// <summary>
    /// Distance at which the warning must already be up: the worse of a comfortable slowdown and
    /// the guaranteed heavy-application room, plus <see cref="WarningTimeMarginFactor"/>.
    /// </summary>
    public static float PlanningDistanceMeters(
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent,
        string? locomotiveTypeId)
    {
        var comfortable = SpeedDisplay.ToMetersPerSecond(speedKmh)
                          * EstimatedSlowdownTimeSeconds(
                              speedKmh, targetKmh, massTonnes, gradePercent, locomotiveTypeId);
        var guaranteed = GuaranteedStopDistanceMeters(
            speedKmh, targetKmh, massTonnes, gradePercent, locomotiveTypeId);
        return Math.Max(comfortable, guaranteed) * WarningTimeMarginFactor;
    }

    /// <summary>
    /// Route distance needed to discover a 30 km/h restriction before its window opens.
    /// Extreme-grade scans are capped for runtime safety.
    /// </summary>
    public static float WarningLookaheadMeters(
        float speedKmh,
        float massTonnes,
        float gradePercent,
        string? locomotiveTypeId)
    {
        var planning = PlanningDistanceMeters(
            speedKmh,
            WarningLookaheadTargetKmh,
            massTonnes,
            gradePercent,
            locomotiveTypeId);
        return Math.Min(planning, MaxWarningLookaheadMeters);
    }

    /// <summary>
    /// Tightest board anywhere in the ahead scan whose planning window is open — an intermediate
    /// 60 must not hide a farther 30.
    /// <para>
    /// Qualification is against our <b>speed</b>, never against the Limit chip: the chip adopting
    /// the restriction is what makes the warning relevant, not redundant (0.5.59 silenced the chip
    /// at the exact frame Limit adopted 30, 1 780 m out on a −2.6 % descent).
    /// </para>
    /// <paramref name="latchedTargetKmh"/> is the target already on screen; it keeps a wider
    /// window so edge wobble cannot blink it away.
    /// </summary>
    public static AheadBoard? SelectEarlyTarget(
        IReadOnlyList<AheadBoard>? aheadBoards,
        float? speedKmh,
        float? massTonnes,
        float gradePercent,
        string? locomotiveTypeId,
        float? latchedTargetKmh = null)
    {
        if (speedKmh is not float speed || aheadBoards == null)
        {
            return null;
        }

        var mass = massTonnes ?? HeavyConsistTonnes;
        AheadBoard? best = null;
        for (var i = 0; i < aheadBoards.Count; i++)
        {
            var board = aheadBoards[i];
            if (board.AlongMeters <= 0f || board.Kmh + MinTargetDeltaKmh >= speed)
            {
                continue;
            }

            var window = Math.Min(
                PlanningDistanceMeters(
                    speed,
                    board.Kmh,
                    mass,
                    gradePercent,
                    locomotiveTypeId),
                MaxWarningLookaheadMeters);
            if (latchedTargetKmh is float latched && Math.Abs(board.Kmh - latched) < 0.5f)
            {
                window *= WarningLatchReleaseFactor;
            }

            if (board.AlongMeters > window)
            {
                continue;
            }

            if (best is null
                || board.Kmh < best.Value.Kmh - 0.5f
                || (Math.Abs(board.Kmh - best.Value.Kmh) < 0.5f
                    && board.AlongMeters < best.Value.AlongMeters))
            {
                best = board;
            }
        }

        return best;
    }

    /// <summary>How long a target survives without being re-seen by the scan.</summary>
    public const float MaxCoastSeconds = 2f;

    /// <summary>
    /// Carry the previous target through a frame where the scan lost its board, closing the
    /// distance at current speed. Gives up once the grace window expires, we are slow enough for
    /// it, or it would be behind us.
    /// </summary>
    public static AheadBoard? CoastTarget(
        float? heldKmh,
        float? heldAlongMeters,
        float coastedSeconds,
        float speedKmh)
    {
        if (heldKmh is not float kmh
            || heldAlongMeters is not float along
            || coastedSeconds > MaxCoastSeconds
            || speedKmh <= kmh + MinTargetDeltaKmh)
        {
            return null;
        }

        var closed = SpeedDisplay.ToMetersPerSecond(speedKmh) * Math.Max(0f, coastedSeconds);
        var remaining = along - closed;
        return remaining <= 0f ? null : new AheadBoard(kmh, remaining);
    }

    private static float TypeFactor(string? locomotiveTypeId) =>
        IsCalibratedDe2(locomotiveTypeId) ? 1f : UncalibratedLocoDecelerationFactor;

    private static bool IsCalibratedDe2(string? locomotiveTypeId) =>
        locomotiveTypeId?.IndexOf("DE2", StringComparison.OrdinalIgnoreCase) >= 0;

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
        float? gradePercent = null,
        string? locomotiveTypeId = null)
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

        // Either window may open the chip, so a target chosen by SelectEarlyTarget is never
        // silenced here — two windows disagreeing is what made the chip blink (0.5.59).
        var planning = PlanningDistanceMeters(speed, target, mass, grade, locomotiveTypeId);
        if (eta > softTime * AdvisoryFactor && distance > planning)
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
