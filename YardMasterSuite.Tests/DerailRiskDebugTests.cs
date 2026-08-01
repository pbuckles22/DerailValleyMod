using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class DerailRiskDebugTests
{
    [Fact]
    public void Format_shows_live_stress_vs_thresholds_and_curve_proxies()
    {
        var text = DerailRiskDebug.Format(
            stress: 50f,
            derailBuildUp: 10f,
            stressThreshold: 500f,
            buildUpThreshold: 100f,
            speedKmh: 60f,
            currentGeometryLimitKmh: 80f,
            aheadLimitKmh: 30f,
            aheadAlongMeters: 1200f);

        Assert.Contains("stress=50/500(10%)", text);
        Assert.Contains("build=10/100(10%)", text);
        Assert.Contains("curveNow=75%", text);
        Assert.Contains("curveAhead=200%@1200m", text);
    }

    [Fact]
    public void Format_keeps_raw_stress_when_threshold_is_unusable()
    {
        // 0.5.64 bug: thr=0 produced four-digit percents — raw pair + thr? marker instead.
        var text = DerailRiskDebug.Format(
            stress: 5f,
            derailBuildUp: 0.1f,
            stressThreshold: 0f,
            buildUpThreshold: 1f,
            speedKmh: 50f,
            currentGeometryLimitKmh: 60f,
            aheadLimitKmh: null,
            aheadAlongMeters: null);

        Assert.Contains("stress=5/0(thr?)", text);
        Assert.Contains("build=0.1/1(10%)", text);
    }

    [Fact]
    public void Format_uses_dashes_when_inputs_missing()
    {
        var text = DerailRiskDebug.Format(
            stress: null,
            derailBuildUp: null,
            stressThreshold: null,
            buildUpThreshold: null,
            speedKmh: 50f,
            currentGeometryLimitKmh: null,
            aheadLimitKmh: null,
            aheadAlongMeters: null);

        Assert.Equal("stress=— build=— curveNow=— curveAhead=—", text);
    }

    [Fact]
    public void SelectAheadCurveBoard_prefers_tightest_geometry_board()
    {
        var board = DerailRiskDebug.SelectAheadCurveBoard(new[]
        {
            new AheadBoard(60f, 200f),
            new AheadBoard(30f, 2500f, fromGeometry: true),
            new AheadBoard(40f, 800f, fromGeometry: true),
        });

        Assert.NotNull(board);
        Assert.Equal(30f, board!.Value.Kmh);
        Assert.True(board.Value.FromGeometry);
    }

    [Fact]
    public void SelectAheadCurveBoard_falls_back_to_posted_when_no_geometry()
    {
        var board = DerailRiskDebug.SelectAheadCurveBoard(new[]
        {
            new AheadBoard(70f, 100f),
            new AheadBoard(40f, 900f),
        });

        Assert.NotNull(board);
        Assert.Equal(40f, board!.Value.Kmh);
        Assert.False(board.Value.FromGeometry);
    }

    [Fact]
    public void CurvePercent_is_100_at_the_limit_and_over_when_faster()
    {
        Assert.Equal("100%", DerailRiskDebug.CurvePercent(30f, 30f));
        Assert.Equal("177%", DerailRiskDebug.CurvePercent(53f, 30f));
        Assert.Equal("—", DerailRiskDebug.CurvePercent(53f, null));
    }
}
