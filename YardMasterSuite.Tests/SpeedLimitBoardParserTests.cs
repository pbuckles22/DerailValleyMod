using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitBoardParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("200")]
    public void ParseKmh_unknown_for_invalid(string? text)
    {
        Assert.Null(SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("6", 60f)]
    [InlineData("8", 80f)]
    [InlineData("12", 120f)]
    [InlineData("1", 10f)]
    public void ParseKmh_digits_times_ten(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("30", 30f)]
    [InlineData("60", 60f)]
    [InlineData("80", 80f)]
    [InlineData("100", 100f)]
    [InlineData("120", 120f)]
    public void ParseKmh_full_kmh_passthrough(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Fact]
    public void ParseKmh_slash_through_and_non_speed_second_line()
    {
        Assert.Equal(80f, SpeedLimitBoardParser.ParseKmh("8\nextra"));
        Assert.Equal(60f, SpeedLimitBoardParser.ParseKmh("6/4"));
    }

    [Fact]
    public void ParseDual_and_Pick_through_vs_diverge()
    {
        var dual = SpeedLimitBoardParser.ParseDual("6/4");
        Assert.NotNull(dual);
        Assert.True(dual!.Value.IsDual);
        Assert.Equal(60f, dual.Value.ThroughKmh);
        Assert.Equal(40f, dual.Value.DivergeKmh);
        Assert.Equal(60f, SpeedLimitBoardParser.Pick(dual.Value, diverging: false));
        Assert.Equal(40f, SpeedLimitBoardParser.Pick(dual.Value, diverging: true));
    }

    [Theory]
    [InlineData("3 4", 30f, 40f)]
    [InlineData("3\n4", 30f, 40f)]
    [InlineData("3\r\n4", 30f, 40f)]
    public void ParseDual_space_and_newline_as_switch(string text, float through, float diverge)
    {
        var dual = SpeedLimitBoardParser.ParseDual(text);
        Assert.NotNull(dual);
        Assert.True(dual!.Value.IsDual);
        Assert.Equal(through, dual.Value.ThroughKmh);
        Assert.Equal(diverge, dual.Value.DivergeKmh);
        Assert.True(SpeedLimitBoardParser.IsSwitchSign(text));
    }

    [Theory]
    [InlineData("4 -1.9", 40f)]
    [InlineData("6\n+1.2", 60f)]
    [InlineData("6 +2", 60f)]
    public void ParseDual_ignores_grade_annotation(string text, float through)
    {
        var dual = SpeedLimitBoardParser.ParseDual(text);
        Assert.NotNull(dual);
        Assert.False(dual!.Value.IsDual);
        Assert.Equal(through, dual.Value.ThroughKmh);
        Assert.False(SpeedLimitBoardParser.IsSwitchSign(text));
    }

    [Fact]
    public void IsSwitchSign_detects_dual_slash()
    {
        Assert.True(SpeedLimitBoardParser.IsSwitchSign("6/4"));
        Assert.False(SpeedLimitBoardParser.IsSwitchSign("6"));
        Assert.False(SpeedLimitBoardParser.IsSwitchSign("6\n+1.2"));
    }
}

public class SpeedLimitBoardFacingTests
{
    // Travel is +Z throughout, so "right of travel" is +X and a board turned toward us has
    // forward −Z (fDot ≈ −1). Deltas are (X = lateral, Z = along).
    private static SpeedLimitBoardFacing.Eval Board(
        float lateral,
        float along,
        float signForwardZ = -1f,
        bool isSwitchSign = false,
        bool junctionNearby = false,
        bool onOurTrack = false,
        bool trackKnown = false) =>
        SpeedLimitBoardFacing.Evaluate(
            signForwardX: 0f, signForwardZ: signForwardZ,
            signRightX: 1f, signRightZ: 0f,
            travelForwardX: 0f, travelForwardZ: 1f,
            deltaToSignX: lateral, deltaToSignZ: along,
            isSwitchSign, junctionNearby, onOurTrack, trackKnown);

    [Fact]
    public void Mainline_governs_board_on_right_turned_toward_us()
    {
        var ok = Board(lateral: 2f, along: -5f);
        Assert.True(ok.Governs);
        Assert.Equal(SpeedLimitBoardFacing.KindMainline, ok.Kind);
        Assert.True(ok.ForwardDot < 0f);
    }

    /// <summary>
    /// 0.5.42 regression: the board alongside the loco read fDot=−1 and was rejected by a rule
    /// demanding fDot ≥ +0.5, so no board ever governed.
    /// </summary>
    [Fact]
    public void Board_alongside_facing_us_governs()
    {
        Assert.True(Board(lateral: 2f, along: -0.4f).Governs);
    }

    [Fact]
    public void Mainline_rejects_board_facing_away()
    {
        // Opposite direction's board: faces the same way we travel, and sits on our left.
        Assert.False(Board(lateral: 2f, along: -5f, signForwardZ: 1f).Governs);
        Assert.False(Board(lateral: -2f, along: -5f, signForwardZ: 1f).Governs);
    }

    [Fact]
    public void Mainline_rejects_board_on_left()
    {
        Assert.False(Board(lateral: -2f, along: -5f).Governs);
    }

    [Fact]
    public void Track_identity_overrides_lateral_distance()
    {
        // Curve inflates lateral far past the fallback corridor, but it is our track.
        var ours = Board(lateral: 45f, along: -5f, onOurTrack: true, trackKnown: true);
        Assert.True(ours.Governs);
        Assert.True(ours.OnOurTrack);

        // Parallel yard track right beside us — small lateral, wrong track.
        var theirs = Board(lateral: 2f, along: -5f, onOurTrack: false, trackKnown: true);
        Assert.False(theirs.Governs);
        Assert.True(theirs.TrackKnown);
    }

    [Fact]
    public void Corridor_is_the_fallback_when_track_is_unresolved()
    {
        var ghost = Board(lateral: 144.6f, along: -1.4f);
        Assert.False(ghost.Governs);
        Assert.False(ghost.TrackKnown);
        Assert.True(ghost.LateralMeters > ghost.MaxLateralMeters);

        // Same board 213 m out: corridor has widened, so it is not dropped mid-approach.
        Assert.True(Board(lateral: 20f, along: 213f).Governs);
    }

    [Fact]
    public void Corridor_widens_with_along_distance_but_is_capped()
    {
        Assert.Equal(
            SpeedLimitBoardFacing.MaxRightLateralMeters,
            SpeedLimitBoardFacing.MaxLateralFor(0f));
        Assert.True(
            SpeedLimitBoardFacing.MaxLateralFor(-250f) > SpeedLimitBoardFacing.MaxLateralFor(-50f));
        Assert.Equal(
            SpeedLimitBoardFacing.MaxLateralCeilingMeters,
            SpeedLimitBoardFacing.MaxLateralFor(5000f));
    }

    [Fact]
    public void Switch_dual_at_junction_skips_right_hand_rule()
    {
        var sw = Board(
            lateral: -2f,
            along: 10f,
            isSwitchSign: true,
            junctionNearby: true,
            onOurTrack: true,
            trackKnown: true);
        Assert.True(sw.Governs);
        Assert.Equal(SpeedLimitBoardFacing.KindSwitch, sw.Kind);
    }

    [Fact]
    public void Switch_dual_needs_a_junction_and_our_track()
    {
        var noJunction = Board(
            lateral: -2f, along: 10f,
            isSwitchSign: true, junctionNearby: false,
            onOurTrack: true, trackKnown: true);
        Assert.False(noJunction.Governs);

        var otherTrack = Board(
            lateral: -2f, along: 10f,
            isSwitchSign: true, junctionNearby: true,
            onOurTrack: false, trackKnown: true);
        Assert.False(otherTrack.Governs);
    }

    [Fact]
    public void Switch_dual_facing_away_does_not_govern()
    {
        var away = Board(
            lateral: -2f, along: 10f, signForwardZ: 1f,
            isSwitchSign: true, junctionNearby: true,
            onOurTrack: true, trackKnown: true);
        Assert.False(away.Governs);
    }
}
