using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Compact derail / tip-over risk fragment for <c>T2 limit</c> tuning.
/// <para>
/// Derail Valley exposes no dedicated rollover API. Live risk comes from coupler-joint
/// <c>TrainStress</c> vs <c>GameParams.DerailStressThreshold</c> /
/// <c>DerailBuildUpThreshold</c>. Predictive "upcoming" risk is the SignPlacer curve ladder
/// we already mirror: current speed vs current / ahead geometry limits.
/// </para>
/// </summary>
public static class DerailRiskDebug
{
    /// <summary>
    /// Thresholds below this are treated as unset (0.5.64 logged <c>thr=0/1</c> and bogus
    /// four-digit stress percents — always keep the raw pair for calibration).
    /// </summary>
    public const float MinUsableThreshold = 0.05f;

    /// <summary>
    /// <c>stress=raw/thr(pct) build=… curveNow=… curveAhead=…</c>
    /// Missing inputs become <c>—</c>.
    /// </summary>
    public static string Format(
        float? stress,
        float? derailBuildUp,
        float? stressThreshold,
        float? buildUpThreshold,
        float? speedKmh,
        float? currentGeometryLimitKmh,
        float? aheadLimitKmh,
        float? aheadAlongMeters)
    {
        var stressTag = FormatPair(stress, stressThreshold);
        var buildTag = FormatPair(derailBuildUp, buildUpThreshold);
        var curveNow = CurvePercent(speedKmh, currentGeometryLimitKmh);
        var curveAhead = aheadLimitKmh is float al && aheadAlongMeters is float along
            ? $"{CurvePercent(speedKmh, al)}@{along:0}m"
            : "—";

        return $"stress={stressTag} build={buildTag} curveNow={curveNow} curveAhead={curveAhead}";
    }

    /// <summary>
    /// Tightest geometry-synthesized board ahead (SignPlacer ladder proxy for tip-over).
    /// Falls back to the tightest posted board when no geometry board is in the scan.
    /// </summary>
    public static AheadBoard? SelectAheadCurveBoard(IReadOnlyList<AheadBoard>? aheadBoards)
    {
        if (aheadBoards == null || aheadBoards.Count == 0)
        {
            return null;
        }

        AheadBoard? bestGeo = null;
        AheadBoard? bestAny = null;
        for (var i = 0; i < aheadBoards.Count; i++)
        {
            var board = aheadBoards[i];
            if (board.AlongMeters <= 0f)
            {
                continue;
            }

            if (bestAny is null
                || board.Kmh + 0.5f < bestAny.Value.Kmh
                || (Math.Abs(board.Kmh - bestAny.Value.Kmh) < 0.5f
                    && board.AlongMeters < bestAny.Value.AlongMeters))
            {
                bestAny = board;
            }

            if (!board.FromGeometry)
            {
                continue;
            }

            if (bestGeo is null
                || board.Kmh + 0.5f < bestGeo.Value.Kmh
                || (Math.Abs(board.Kmh - bestGeo.Value.Kmh) < 0.5f
                    && board.AlongMeters < bestGeo.Value.AlongMeters))
            {
                bestGeo = board;
            }
        }

        return bestGeo ?? bestAny;
    }

    /// <summary>speed / limit as a percent (100 = at the board; &gt;100 = over).</summary>
    public static string CurvePercent(float? speedKmh, float? limitKmh)
    {
        if (speedKmh is not float speed || limitKmh is not float limit || limit <= 0.5f)
        {
            return "—";
        }

        var pct = (int)Math.Round((speed / limit) * 100f, MidpointRounding.AwayFromZero);
        return $"{pct}%";
    }

    /// <summary>value / threshold as a percent (100 = at derail stress/build threshold).</summary>
    public static string RatioPercent(float? value, float? threshold)
    {
        if (value is not float v || threshold is not float thr || thr < MinUsableThreshold)
        {
            return "—";
        }

        var pct = (int)Math.Round((v / thr) * 100f, MidpointRounding.AwayFromZero);
        return $"{pct}%";
    }

    /// <summary>Always keeps raw numbers; percent only when the threshold looks usable.</summary>
    public static string FormatPair(float? value, float? threshold)
    {
        if (value is not float v)
        {
            return "—";
        }

        if (threshold is not float thr)
        {
            return $"{v:0.##}/—";
        }

        if (thr < MinUsableThreshold)
        {
            return $"{v:0.##}/{thr:0.##}(thr?)";
        }

        var pct = (int)Math.Round((v / thr) * 100f, MidpointRounding.AwayFromZero);
        return $"{v:0.##}/{thr:0.##}({pct}%)";
    }
}
