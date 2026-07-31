using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One governing board ahead of the loco (km/h + along-track meters).</summary>
public readonly struct AheadBoard
{
    public AheadBoard(float kmh, float alongMeters)
    {
        Kmh = kmh;
        AlongMeters = alongMeters;
    }

    public float Kmh { get; }
    public float AlongMeters { get; }
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
    /// The soft distance already carries reaction time, so the margin here is small — the yellow
    /// Brake chip (<see cref="BrakeAdvisory.AdvisoryFactor"/>) is what warns far out, not the number.
    /// </summary>
    public const float AdoptLeadFactor = 1.15f;

    /// <summary>
    /// Keep a previously adopted board until this multiple of soft distance (hysteresis).
    /// Grade/speed wobble at the adopt edge caused 30↔60 Recommended flicker without this gap.
    /// </summary>
    public const float ReleaseLeadFactor = 1.55f;

    /// <summary>
    /// When already at/under an upcoming board's number, still show it for this many seconds
    /// of travel at current speed (floored at <see cref="MinHoldAheadMeters"/>).
    /// </summary>
    public const float HoldAheadSeconds = 60f;

    /// <summary>Floor for hold-ahead distance when nearly stopped.</summary>
    public const float MinHoldAheadMeters = 400f;

    /// <summary>
    /// Recommended km/h for the Limit chip, or null when nothing is known.
    /// <paramref name="adoptedAlongMeters"/> is the distance to the board that set the number
    /// when look-ahead adopted; otherwise null (posted / geometry only).
    /// <paramref name="gradePercent"/> lengthens the lead on a descent (downhill gravity).
    /// <paramref name="stickyAdoptedKmh"/> prior look-ahead adopt — uses <see cref="ReleaseLeadFactor"/>
    /// so a far restriction does not chatter at the soft-lead edge.
    /// </summary>
    public static float? Resolve(
        float? postedKmh,
        IReadOnlyList<AheadBoard>? aheadBoards,
        float? geometryKmh,
        float? speedKmh,
        float? massTonnes,
        out float? adoptedAlongMeters,
        float gradePercent = 0f,
        float? stickyAdoptedKmh = null)
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
                if (speed > board.Kmh + 0.5f)
                {
                    var factor = stickyMatch ? ReleaseLeadFactor : AdoptLeadFactor;
                    var lead =
                        BrakeAdvisory.RequiredDistanceMeters(speed, board.Kmh, mass, gradePercent)
                        * factor;
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

                    if (board.AlongMeters > hold)
                    {
                        continue;
                    }
                }

                recommended = board.Kmh;
                adoptedAlongMeters = board.AlongMeters;
            }
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
