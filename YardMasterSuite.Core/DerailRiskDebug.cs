using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Compact derail / tip-over risk fragment for <c>T2 limit</c> (and future Stress chip).
/// Live risk: coupler <c>TrainStress</c> vs game derail thresholds.
/// Curve proxy: current speed vs ahead posted board (no geometry synthesis).
/// </summary>
public static class DerailRiskDebug
{
    public const float MinUsableThreshold = 0.05f;

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

    /// <summary>Tightest posted board ahead (for curveAhead debug).</summary>
    public static AheadBoard? SelectAheadCurveBoard(IReadOnlyList<AheadBoard>? aheadBoards)
    {
        if (aheadBoards == null || aheadBoards.Count == 0)
        {
            return null;
        }

        AheadBoard? best = null;
        for (var i = 0; i < aheadBoards.Count; i++)
        {
            var board = aheadBoards[i];
            if (board.AlongMeters <= 0f)
            {
                continue;
            }

            if (best is null
                || board.Kmh + 0.5f < best.Value.Kmh
                || (Math.Abs(board.Kmh - best.Value.Kmh) < 0.5f
                    && board.AlongMeters < best.Value.AlongMeters))
            {
                best = board;
            }
        }

        return best;
    }

    public static string CurvePercent(float? speedKmh, float? limitKmh)
    {
        if (speedKmh is not float speed || limitKmh is not float limit || limit <= 0.5f)
        {
            return "—";
        }

        var pct = (int)Math.Round((speed / limit) * 100f, MidpointRounding.AwayFromZero);
        return $"{pct}%";
    }

    public static string RatioPercent(float? value, float? threshold)
    {
        if (value is not float v || threshold is not float thr || thr < MinUsableThreshold)
        {
            return "—";
        }

        var pct = (int)Math.Round((v / thr) * 100f, MidpointRounding.AwayFromZero);
        return $"{pct}%";
    }

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
