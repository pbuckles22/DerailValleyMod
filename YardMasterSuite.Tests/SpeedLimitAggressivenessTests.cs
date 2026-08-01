using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Tweaking dial for 1.16: 0 = current sticky-safe Limit, 1 = late Limit (pre-overcorrection).
/// Brake's geometry-ahead scan stays fully on at every setting — only Limit timing moves.
/// </summary>
public class SpeedLimitAggressivenessTests
{
    [Fact]
    public void Dial_is_between_zero_and_one()
    {
        Assert.InRange(SpeedLimitAggressiveness.Value, 0f, 1f);
    }

    [Fact]
    public void Safe_endpoints_match_the_pre_dial_sticky_constants()
    {
        Assert.Equal(1.15f, SpeedLimitAggressiveness.SafeAdoptLeadFactor);
        Assert.Equal(1.55f, SpeedLimitAggressiveness.SafeReleaseLeadFactor);
        Assert.Equal(60f, SpeedLimitAggressiveness.SafeHoldAheadSeconds);
        Assert.Equal(400f, SpeedLimitAggressiveness.SafeMinHoldAheadMeters);
        Assert.Equal(1f, SpeedLimitAggressiveness.SafeGeometryLimitLeadScale);
    }

    [Fact]
    public void Late_endpoints_are_strictly_less_sticky_than_safe()
    {
        Assert.True(SpeedLimitAggressiveness.LateAdoptLeadFactor
                    < SpeedLimitAggressiveness.SafeAdoptLeadFactor);
        Assert.True(SpeedLimitAggressiveness.LateReleaseLeadFactor
                    < SpeedLimitAggressiveness.SafeReleaseLeadFactor);
        Assert.True(SpeedLimitAggressiveness.LateHoldAheadSeconds
                    < SpeedLimitAggressiveness.SafeHoldAheadSeconds);
        Assert.True(SpeedLimitAggressiveness.LateMinHoldAheadMeters
                    < SpeedLimitAggressiveness.SafeMinHoldAheadMeters);
        Assert.True(SpeedLimitAggressiveness.LateGeometryLimitLeadScale
                    < SpeedLimitAggressiveness.SafeGeometryLimitLeadScale);
    }

    [Fact]
    public void Active_factors_lerp_between_safe_and_late()
    {
        var t = SpeedLimitAggressiveness.Value;
        Assert.Equal(
            Lerp(
                SpeedLimitAggressiveness.SafeAdoptLeadFactor,
                SpeedLimitAggressiveness.LateAdoptLeadFactor,
                t),
            RecommendedSpeedLimit.AdoptLeadFactor,
            precision: 4);
        Assert.Equal(
            Lerp(
                SpeedLimitAggressiveness.SafeReleaseLeadFactor,
                SpeedLimitAggressiveness.LateReleaseLeadFactor,
                t),
            RecommendedSpeedLimit.ReleaseLeadFactor,
            precision: 4);
        Assert.Equal(
            Lerp(
                SpeedLimitAggressiveness.SafeGeometryLimitLeadScale,
                SpeedLimitAggressiveness.LateGeometryLimitLeadScale,
                t),
            SpeedLimitAggressiveness.GeometryLimitLeadScale,
            precision: 4);
    }

    [Fact]
    public void FormatTuneDetail_includes_dial_factors_and_geometry_adopt_source()
    {
        var detail = SpeedLimitAggressiveness.FormatTuneDetail(
            adoptedKmh: 30f,
            adoptedAlongMeters: 800f,
            aheadBoards: new[] { new AheadBoard(30f, 800f, fromGeometry: true) },
            speedKmh: 55f,
            massTonnes: 37.8f,
            gradePercent: -1.5f);

        Assert.Contains($"agg={SpeedLimitAggressiveness.Value:0.00}", detail);
        Assert.Contains($"geoScale={SpeedLimitAggressiveness.GeometryLimitLeadScale:0.00}", detail);
        Assert.Contains($"adopt={RecommendedSpeedLimit.AdoptLeadFactor:0.00}", detail);
        Assert.Contains($"release={RecommendedSpeedLimit.ReleaseLeadFactor:0.00}", detail);
        Assert.Contains("src=geo", detail);
        Assert.Contains("along=800", detail);
        Assert.Contains("lead=", detail);
    }

    [Fact]
    public void FormatTuneDetail_marks_posted_boards_separately_from_geometry()
    {
        var detail = SpeedLimitAggressiveness.FormatTuneDetail(
            adoptedKmh: 40f,
            adoptedAlongMeters: 300f,
            aheadBoards: new[] { new AheadBoard(40f, 300f) },
            speedKmh: 50f,
            massTonnes: 37.8f,
            gradePercent: 0f);

        Assert.Contains("src=posted", detail);
        Assert.DoesNotContain("src=geo", detail);
    }

    [Fact]
    public void Limit_lead_scale_applies_to_posted_and_geometry_boards_alike()
    {
        const float speed = 50f;
        const float mass = 37.8f;
        const float grade = -1.5f;
        var soft = BrakeAdvisory.RequiredDistanceMeters(speed, 30f, mass, grade);
        var lead = soft
                   * RecommendedSpeedLimit.AdoptLeadFactor
                   * SpeedLimitAggressiveness.LimitLeadScale;
        if (SpeedLimitAggressiveness.LimitLeadScale >= 0.999f)
        {
            return;
        }

        // Just outside the scaled lead — neither posted nor geometry may adopt.
        var along = lead + 50f;
        Assert.Equal(
            80f,
            RecommendedSpeedLimit.Resolve(
                80f, new[] { new AheadBoard(30f, along) }, null, speed, mass, out _, grade));
        Assert.Equal(
            80f,
            RecommendedSpeedLimit.Resolve(
                80f,
                new[] { new AheadBoard(30f, along, fromGeometry: true) },
                null,
                speed,
                mass,
                out _,
                grade));
    }

    [Fact]
    public void Dial_releases_the_logged_0_5_64_geo_sticky_case()
    {
        // Live: along=2665, lead=3556 at agg=0.2 → soft≈4078. Current dial must clear it.
        const float along = 2665f;
        const float soft = 4078f;
        Assert.True(
            along > SpeedLimitAggressiveness.LeadAt(soft, SpeedLimitAggressiveness.Value, true),
            $"agg={SpeedLimitAggressiveness.Value} still adopts geo30@2665");
    }

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);
}
