using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Log-driven dial suggestion: given a sticky geometry board (along &lt; lead), find the
/// smallest aggressiveness that would have released Limit — so smoke can jump to 0.5 instead
/// of inching 0.1 → 0.2 → …
/// </summary>
public class SpeedLimitSuggestTests
{
    [Fact]
    public void Suggest_is_null_when_already_outside_the_lead()
    {
        // Soft distance 2000 m; at agg=0 lead = 2000*1.15*1 = 2300; along 2500 is free.
        Assert.Null(SpeedLimitAggressiveness.SuggestMinimumToRelease(
            alongMeters: 2500f,
            softRequiredDistanceMeters: 2000f,
            fromGeometry: true));
    }

    [Fact]
    public void Suggest_for_0_5_64_sticky_geo_case_is_around_half()
    {
        // Live 0.5.64: along=2665, lead=3556 at agg=0.2 (geoScale=0.8, adopt=1.09)
        // => soft ≈ 3556 / (1.09*0.8) ≈ 4078. At 0.5: lead ≈ 4078*1.0*0.5 ≈ 2039 &lt; 2665.
        const float along = 2665f;
        const float soft = 4078f;
        var suggest = SpeedLimitAggressiveness.SuggestMinimumToRelease(along, soft, fromGeometry: true);
        Assert.NotNull(suggest);
        Assert.InRange(suggest!.Value, 0.35f, 0.55f);
        Assert.True(along > SpeedLimitAggressiveness.LeadAt(soft, suggest.Value, fromGeometry: true));
        Assert.True(along <= SpeedLimitAggressiveness.LeadAt(soft, 0.2f, fromGeometry: true));
    }

    [Fact]
    public void FormatTuneDetail_includes_headroom_and_suggest_keys()
    {
        var detail = SpeedLimitAggressiveness.FormatTuneDetail(
            adoptedKmh: 30f,
            adoptedAlongMeters: 800f,
            aheadBoards: new[] { new AheadBoard(30f, 800f, fromGeometry: true) },
            speedKmh: 70f,
            massTonnes: 37.8f,
            gradePercent: -2f);

        Assert.Contains("headroom=", detail);
        Assert.Contains("suggest=", detail);
    }
}
