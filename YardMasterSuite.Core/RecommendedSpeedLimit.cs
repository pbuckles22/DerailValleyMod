using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One governing board ahead of the loco (km/h + along-track meters).</summary>
public readonly struct AheadBoard
{
    public AheadBoard(float kmh, float alongMeters, bool fromGeometry = false)
    {
        Kmh = kmh;
        AlongMeters = alongMeters;
        FromGeometry = fromGeometry;
    }

    public float Kmh { get; }
    public float AlongMeters { get; }

    /// <summary>
    /// True when synthesized from curve radius (not a streamed <c>SignDebug</c> prop).
    /// Limit adopt may require these closer via <see cref="SpeedLimitAggressiveness.GeometryLimitLeadScale"/>;
    /// Brake always treats them like any other board.
    /// </summary>
    public bool FromGeometry { get; }
}

/// <summary>
/// Look-ahead recommended Limit for the HUD (**1.16**).
/// <para>
/// Posted boards are the authored authority. Among <b>all</b> slower boards ahead inside the
/// soft-brake lead, Limit adopts the tightest number — not only the nearest board — so an
/// intermediate 80 cannot hide a 60 until it is too late to brake.
/// Geometry fills only when no posted board is known.
/// </para>
/// </summary>
public static class RecommendedSpeedLimit
{
    /// <summary>
    /// Adopt a slower board this many multiples of the soft required distance out.
    /// Driven by <see cref="SpeedLimitAggressiveness"/> (0 = sticky-safe, 1 = late).
    /// </summary>
    public static float AdoptLeadFactor => SpeedLimitAggressiveness.AdoptLeadFactor;

    /// <summary>
    /// Keep a previously adopted board until this multiple of soft distance (hysteresis).
    /// Driven by <see cref="SpeedLimitAggressiveness"/>.
    /// </summary>
    public static float ReleaseLeadFactor => SpeedLimitAggressiveness.ReleaseLeadFactor;

    /// <summary>
    /// Sticky release lead is computed as if still at least board+margin km/h so slowing
    /// toward the restriction (correct braking) cannot shrink the lead and un-adopt (50↔60).
    /// </summary>
    public const float StickyReleaseSpeedMarginKmh = 10f;

    /// <summary>
    /// When already at/under an upcoming board's number, still show it for this many seconds
    /// of travel at current speed (floored at <see cref="MinHoldAheadMeters"/>).
    /// </summary>
    public static float HoldAheadSeconds => SpeedLimitAggressiveness.HoldAheadSeconds;

    /// <summary>Floor for hold-ahead distance when nearly stopped.</summary>
    public static float MinHoldAheadMeters => SpeedLimitAggressiveness.MinHoldAheadMeters;

    /// <summary>
    /// Recommended km/h for the Limit chip, or null when nothing is known.
    /// <paramref name="adoptedAlongMeters"/> is the distance to the board that set the number
    /// when look-ahead adopted; otherwise null (posted / geometry only).
    /// <paramref name="gradePercent"/> lengthens the lead on a descent (downhill gravity).
    /// <paramref name="stickyAdoptedKmh"/> prior look-ahead adopt — uses <see cref="ReleaseLeadFactor"/>
    /// so a far restriction does not chatter at the soft-lead edge.
    /// <paramref name="stickyAdoptGradePercent"/> grade when sticky was adopted — release lead uses the
    /// more adverse of current vs adopt grade so easing out of a descent cannot un-adopt (30↔60).
    /// </summary>
    public static float? Resolve(
        float? postedKmh,
        IReadOnlyList<AheadBoard>? aheadBoards,
        float? geometryKmh,
        float? speedKmh,
        float? massTonnes,
        out float? adoptedAlongMeters,
        float gradePercent = 0f,
        float? stickyAdoptedKmh = null,
        float? stickyAdoptGradePercent = null)
    {
        adoptedAlongMeters = null;
        float? recommended = postedKmh;
        if (speedKmh is not float speed)
        {
            if (recommended is null && geometryKmh is float geoOnly)
            {
                recommended = geoOnly;
            }

            return recommended;
        }

        var mass = massTonnes ?? BrakeAdvisory.HeavyConsistTonnes;
        var stickySeen = false;
        if (aheadBoards != null)
        {
            for (var i = 0; i < aheadBoards.Count; i++)
            {
                var board = aheadBoards[i];
                if (board.AlongMeters <= 0f)
                {
                    continue;
                }

                // Only boards slower than the current recommendation (posted or already adopted).
                if (recommended is float rec && board.Kmh + 0.5f >= rec)
                {
                    continue;
                }

                var stickyMatch = stickyAdoptedKmh is float sticky
                    && Math.Abs(board.Kmh - sticky) < 0.5f;
                if (stickyMatch)
                {
                    stickySeen = true;
                }

                // Aggressiveness dial shortens Limit lead for every ahead board; Brake ignores this.
                var leadScale = SpeedLimitAggressiveness.LimitLeadScale;
                if (leadScale <= 0f)
                {
                    continue;
                }

                if (speed > board.Kmh + 0.5f)
                {
                    float lead;
                    if (stickyMatch)
                    {
                        // Floor lead speed so braking toward the board cannot un-adopt.
                        var leadSpeed = Math.Max(speed, board.Kmh + StickyReleaseSpeedMarginKmh);
                        // Keep the more adverse (more downhill) grade from adopt time.
                        var releaseGrade = stickyAdoptGradePercent is float sg
                            ? Math.Min(gradePercent, sg)
                            : gradePercent;
                        lead = BrakeAdvisory.RequiredDistanceMeters(
                                   leadSpeed, board.Kmh, mass, releaseGrade)
                               * ReleaseLeadFactor;
                    }
                    else
                    {
                        lead = BrakeAdvisory.RequiredDistanceMeters(
                                   speed, board.Kmh, mass, gradePercent)
                               * AdoptLeadFactor;
                    }

                    lead *= leadScale;
                    if (board.AlongMeters > lead)
                    {
                        continue;
                    }
                }
                else
                {
                    // Already at/under this number — hold Limit only for ~HoldAheadSeconds of travel.
                    // Sticky keeps a prior adopt a bit farther so it does not blink out at the floor edge.
                    var hold = Math.Max(
                        MinHoldAheadMeters,
                        SpeedDisplay.ToMetersPerSecond(speed) * HoldAheadSeconds);
                    if (stickyMatch)
                    {
                        hold *= ReleaseLeadFactor / AdoptLeadFactor;
                    }

                    hold *= leadScale;
                    if (board.AlongMeters > hold)
                    {
                        continue;
                    }
                }

                recommended = board.Kmh;
                adoptedAlongMeters = board.AlongMeters;
            }
        }

        // Scan drop: sticky board briefly missing from AheadBoards — keep the tighter number.
        if (!stickySeen
            && stickyAdoptedKmh is float stickyKeep
            && (recommended is null || stickyKeep + 0.5f < recommended.Value))
        {
            recommended = stickyKeep;
            adoptedAlongMeters ??= MinHoldAheadMeters;
        }

        if (recommended is null && geometryKmh is float geo)
        {
            recommended = geo;
        }

        return recommended;
    }

    /// <summary>Nearest board ahead whose number differs from <paramref name="fromKmh"/> (trend arrow).</summary>
    public static AheadBoard? NextDifferent(float? fromKmh, IReadOnlyList<AheadBoard>? aheadBoards)
    {
        if (fromKmh is not float from || aheadBoards == null)
        {
            return null;
        }

        var fromWhole = (int)Math.Round(from, MidpointRounding.AwayFromZero);
        AheadBoard? best = null;
        for (var i = 0; i < aheadBoards.Count; i++)
        {
            var board = aheadBoards[i];
            if (board.AlongMeters <= 0f)
            {
                continue;
            }

            var whole = (int)Math.Round(board.Kmh, MidpointRounding.AwayFromZero);
            if (whole == fromWhole)
            {
                continue;
            }

            if (best is null || board.AlongMeters < best.Value.AlongMeters)
            {
                best = board;
            }
        }

        return best;
    }
}
