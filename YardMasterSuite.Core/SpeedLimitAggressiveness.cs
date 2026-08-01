using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Single tweaking dial for how early/sticky the Limit chip adopts look-ahead restrictions (**1.16**).
/// <para>
/// <b>0</b> = sticky-safe Limit (early adopt, long hold).<br/>
/// <b>1</b> = late Limit / little sticky.<br/>
/// Brake's geometry-ahead scan stays fully on. When Brake is actively targeting a board,
/// <see cref="BrakeLimitAlign"/> forces Limit to show that number (no Brake-without-Limit).
/// The dial still governs Limit when Brake is silent, and shortens Limit lead for <b>all</b>
/// ahead boards (posted + geometry) via <see cref="LimitLeadScale"/>.
/// </para>
/// </summary>
public static class SpeedLimitAggressiveness
{
    /// <summary>
    /// Corpus pick for 0.5.68 (sessions 067–067h, 803 frames): dial 0.40 left Limit at
    /// Recommended 30 for most of the trip (497/803) while real take-'3'=30 was rare (~10).
    /// <c>suggest=</c> under high posted peaked ~0.77 (med ~0.69). Jump to <b>0.80</b> so
    /// LimitLeadScale≈0.20 clears sticky Rec30 when Brake is silent; BrakeLimitAlign still
    /// mirrors an active Brake target. Planning margin tightened separately to 10%.
    /// </summary>
    public const float Value = 0.80f;

    public const float SafeAdoptLeadFactor = 1.15f;
    public const float SafeReleaseLeadFactor = 1.55f;
    public const float SafeHoldAheadSeconds = 60f;
    public const float SafeMinHoldAheadMeters = 400f;

    /// <summary>At 0, every ahead board uses the full Limit adopt lead.</summary>
    public const float SafeLimitLeadScale = 1f;

    public const float LateAdoptLeadFactor = 0.80f;
    public const float LateReleaseLeadFactor = 1.05f;
    public const float LateHoldAheadSeconds = 20f;
    public const float LateMinHoldAheadMeters = 120f;

    /// <summary>
    /// At 1, Limit Resolve ignores ahead boards (Brake align still mirrors an active Brake target).
    /// </summary>
    public const float LateLimitLeadScale = 0f;

    // Back-compat names used by older tests / logs.
    public const float SafeGeometryLimitLeadScale = SafeLimitLeadScale;
    public const float LateGeometryLimitLeadScale = LateLimitLeadScale;

    public static float AdoptLeadFactor => AdoptLeadFactorAt(Value);

    public static float ReleaseLeadFactor => ReleaseLeadFactorAt(Value);

    public static float HoldAheadSeconds =>
        Lerp(SafeHoldAheadSeconds, LateHoldAheadSeconds, Value);

    public static float MinHoldAheadMeters =>
        Lerp(SafeMinHoldAheadMeters, LateMinHoldAheadMeters, Value);

    /// <summary>Multiplier on Limit adopt/release lead for every ahead board (posted + geometry).</summary>
    public static float LimitLeadScale => LimitLeadScaleAt(Value);

    /// <summary>Alias for tune logs (<c>geoScale=</c> key kept so prior smoke parsers still work).</summary>
    public static float GeometryLimitLeadScale => LimitLeadScale;

    public static float AdoptLeadFactorAt(float aggressiveness) =>
        Lerp(SafeAdoptLeadFactor, LateAdoptLeadFactor, aggressiveness);

    public static float ReleaseLeadFactorAt(float aggressiveness) =>
        Lerp(SafeReleaseLeadFactor, LateReleaseLeadFactor, aggressiveness);

    public static float LimitLeadScaleAt(float aggressiveness) =>
        Lerp(SafeLimitLeadScale, LateLimitLeadScale, aggressiveness);

    public static float GeometryLimitLeadScaleAt(float aggressiveness) =>
        LimitLeadScaleAt(aggressiveness);

    /// <summary>Limit adopt lead at an arbitrary dial setting: soft × adopt × leadScale.</summary>
    public static float LeadAt(float softRequiredDistanceMeters, float aggressiveness, bool fromGeometry)
    {
        // fromGeometry kept for call-site compatibility; scale applies to all boards now.
        _ = fromGeometry;
        return softRequiredDistanceMeters
               * AdoptLeadFactorAt(aggressiveness)
               * LimitLeadScaleAt(aggressiveness);
    }

    /// <summary>
    /// Smallest dial value that would put <paramref name="alongMeters"/> outside the Limit lead.
    /// Null when already free at 0, or when even aggressiveness 1 still adopts.
    /// </summary>
    public static float? SuggestMinimumToRelease(
        float alongMeters,
        float softRequiredDistanceMeters,
        bool fromGeometry)
    {
        if (alongMeters <= 0f || softRequiredDistanceMeters <= 0f)
        {
            return null;
        }

        if (alongMeters > LeadAt(softRequiredDistanceMeters, 0f, fromGeometry))
        {
            return null;
        }

        if (alongMeters <= LeadAt(softRequiredDistanceMeters, 1f, fromGeometry))
        {
            return null;
        }

        var lo = 0f;
        var hi = 1f;
        for (var i = 0; i < 24; i++)
        {
            var mid = (lo + hi) * 0.5f;
            if (alongMeters > LeadAt(softRequiredDistanceMeters, mid, fromGeometry))
            {
                hi = mid;
            }
            else
            {
                lo = mid;
            }
        }

        return (float)(Math.Ceiling(hi * 100f - 1e-4f) / 100f);
    }

    public static string FormatTuneDetail(
        float? adoptedKmh,
        float? adoptedAlongMeters,
        IReadOnlyList<AheadBoard>? aheadBoards,
        float? speedKmh,
        float? massTonnes,
        float gradePercent,
        float? stickyAdoptedKmh = null,
        float? stickyAdoptGradePercent = null)
    {
        var factors =
            $"agg={Value:0.00} geoScale={LimitLeadScale:0.00} "
            + $"adopt={AdoptLeadFactor:0.00} release={ReleaseLeadFactor:0.00}";

        if (adoptedKmh is not float target || speedKmh is not float speed)
        {
            return $"{factors} src=—";
        }

        var mass = massTonnes ?? BrakeAdvisory.HeavyConsistTonnes;
        AheadBoard? match = null;
        if (aheadBoards != null && adoptedAlongMeters is float alongHint)
        {
            var bestDelta = float.MaxValue;
            for (var i = 0; i < aheadBoards.Count; i++)
            {
                var board = aheadBoards[i];
                if (Math.Abs(board.Kmh - target) >= 0.5f || board.AlongMeters <= 0f)
                {
                    continue;
                }

                var delta = Math.Abs(board.AlongMeters - alongHint);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    match = board;
                }
            }
        }

        if (match is null && aheadBoards != null)
        {
            for (var i = 0; i < aheadBoards.Count; i++)
            {
                var board = aheadBoards[i];
                if (Math.Abs(board.Kmh - target) < 0.5f && board.AlongMeters > 0f)
                {
                    if (match is null || board.AlongMeters < match.Value.AlongMeters)
                    {
                        match = board;
                    }
                }
            }
        }

        string src;
        float along;
        float lead;
        bool fromGeometry;
        if (match is AheadBoard boardMatch)
        {
            fromGeometry = boardMatch.FromGeometry;
            src = fromGeometry ? "geo" : "posted";
            along = boardMatch.AlongMeters;
            lead = LimitLeadMeters(
                boardMatch, speed, target, mass, gradePercent, stickyAdoptedKmh, stickyAdoptGradePercent);
        }
        else if (stickyAdoptedKmh is float sticky && Math.Abs(sticky - target) < 0.5f)
        {
            fromGeometry = false;
            src = "sticky";
            along = adoptedAlongMeters ?? MinHoldAheadMeters;
            lead = MinHoldAheadMeters;
        }
        else
        {
            return $"{factors} src=—";
        }

        var headroom = lead > 1f ? (lead - along) / lead : 0f;
        var soft = SoftRequiredFromLead(lead, fromGeometry);
        var suggest = headroom > 0f
            ? SuggestMinimumToRelease(along, soft, fromGeometry)
            : null;
        var suggestTag = suggest is float s ? $" suggest={s:0.00}" : " suggest=—";
        return $"{factors} src={src} along={along:0} lead={lead:0} headroom={headroom:+0%;-0%;0%}{suggestTag}";
    }

    public static float SoftRequiredFromLead(float leadMeters, bool fromGeometry)
    {
        _ = fromGeometry;
        var denom = AdoptLeadFactor * LimitLeadScale;
        return denom <= 1e-4f ? leadMeters : leadMeters / denom;
    }

    public static float LimitLeadMeters(
        AheadBoard board,
        float speedKmh,
        float targetKmh,
        float massTonnes,
        float gradePercent,
        float? stickyAdoptedKmh = null,
        float? stickyAdoptGradePercent = null)
    {
        var scale = LimitLeadScale;
        if (scale <= 0f)
        {
            return 0f;
        }

        var stickyMatch = stickyAdoptedKmh is float sticky
            && Math.Abs(board.Kmh - sticky) < 0.5f;

        float lead;
        if (speedKmh > targetKmh + 0.5f)
        {
            if (stickyMatch)
            {
                var leadSpeed = Math.Max(speedKmh, targetKmh + RecommendedSpeedLimit.StickyReleaseSpeedMarginKmh);
                var releaseGrade = stickyAdoptGradePercent is float sg
                    ? Math.Min(gradePercent, sg)
                    : gradePercent;
                lead = BrakeAdvisory.RequiredDistanceMeters(
                           leadSpeed, targetKmh, massTonnes, releaseGrade)
                       * ReleaseLeadFactor;
            }
            else
            {
                lead = BrakeAdvisory.RequiredDistanceMeters(
                           speedKmh, targetKmh, massTonnes, gradePercent)
                       * AdoptLeadFactor;
            }
        }
        else
        {
            lead = Math.Max(
                MinHoldAheadMeters,
                SpeedDisplay.ToMetersPerSecond(speedKmh) * HoldAheadSeconds);
            if (stickyMatch)
            {
                lead *= ReleaseLeadFactor / AdoptLeadFactor;
            }
        }

        return lead * scale;
    }

    private static float Lerp(float a, float b, float t)
    {
        var x = t < 0f ? 0f : t > 1f ? 1f : t;
        return a + ((b - a) * x);
    }
}
