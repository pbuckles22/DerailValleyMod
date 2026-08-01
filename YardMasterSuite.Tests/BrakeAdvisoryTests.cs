using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BrakeAdvisoryTests
{
    [Fact]
    public void Heavier_consist_stops_slower()
    {
        var light = BrakeAdvisory.DecelerationFor(40f);
        var heavy = BrakeAdvisory.DecelerationFor(900f);
        Assert.True(light > heavy);
        Assert.Equal(BrakeAdvisory.MaxDecelMps2, light);
        Assert.Equal(BrakeAdvisory.MinDecelMps2, heavy);
    }

    [Fact]
    public void Required_time_grows_with_speed_and_mass()
    {
        var slowLight = BrakeAdvisory.RequiredTimeSeconds(70f, 60f, 38f);
        var fastLight = BrakeAdvisory.RequiredTimeSeconds(90f, 60f, 38f);
        var fastHeavy = BrakeAdvisory.RequiredTimeSeconds(90f, 60f, 900f);
        Assert.True(fastLight > slowLight);
        Assert.True(fastHeavy > fastLight);
    }

    [Fact]
    public void Required_distance_grows_with_speed_delta()
    {
        var small = BrakeAdvisory.RequiredDistanceMeters(70f, 60f, 300f);
        var large = BrakeAdvisory.RequiredDistanceMeters(90f, 60f, 300f);
        Assert.True(large > small);
    }

    [Fact]
    public void Soft_80_to_60_needs_long_lead_even_for_light_loco()
    {
        var required = BrakeAdvisory.RequiredDistanceMeters(80f, 60f, 38f);
        Assert.True(required >= 800f, $"expected soft lead ≥800 m, got {required:0}");
    }

    [Fact]
    public void Faster_train_warns_earlier_at_same_distance()
    {
        const float distance = 900f;
        const float target = 60f;
        const float mass = 38f;
        var slow = BrakeAdvisory.Evaluate(70f, target, distance, mass);
        var fast = BrakeAdvisory.Evaluate(95f, target, distance, mass);
        Assert.True(
            (int)fast.Level >= (int)slow.Level,
            $"fast={fast.Level} should be at least as urgent as slow={slow.Level}");
        Assert.NotEqual(BrakeAdvisoryLevel.None, fast.Level);
    }

    [Fact]
    public void Smoke_case_86_to_60_at_439m_is_critical_when_loaded()
    {
        var state = BrakeAdvisory.Evaluate(
            speedKmh: 86f,
            nextLimitKmh: 60f,
            nextDistanceMeters: 439f,
            massTonnes: 900f);
        Assert.Equal(BrakeAdvisoryLevel.Critical, state.Level);
        Assert.Equal(60, state.TargetKmh);
        Assert.Contains("60", state.Text);
        Assert.Contains("s", state.Text);
    }

    /// <summary>
    /// Retuned from the 0.5.57 DE2 trace: yellow must begin well before 439 m because by 439 m
    /// even a light loco needs a hard application.
    /// </summary>
    [Fact]
    public void Light_loco_warns_early_and_is_critical_by_439m()
    {
        Assert.Equal(BrakeAdvisoryLevel.Advisory, BrakeAdvisory.Evaluate(86f, 60f, 1000f, 38f).Level);
        Assert.Equal(BrakeAdvisoryLevel.Critical, BrakeAdvisory.Evaluate(86f, 60f, 439f, 38f).Level);
    }

    [Fact]
    public void Advisory_appears_well_before_critical()
    {
        var far = BrakeAdvisory.Evaluate(86f, 60f, 12000f, 900f);
        var closing = BrakeAdvisory.Evaluate(86f, 60f, 4500f, 900f);
        var late = BrakeAdvisory.Evaluate(86f, 60f, 300f, 900f);
        Assert.Equal(BrakeAdvisoryLevel.None, far.Level);
        Assert.Equal(BrakeAdvisoryLevel.Advisory, closing.Level);
        Assert.Equal(BrakeAdvisoryLevel.Critical, late.Level);
    }

    [Fact]
    public void Silent_when_next_limit_is_not_slower()
    {
        Assert.Equal(
            BrakeAdvisoryLevel.None,
            BrakeAdvisory.Evaluate(60f, 80f, 200f, 900f).Level);
        Assert.Equal(
            BrakeAdvisoryLevel.None,
            BrakeAdvisory.Evaluate(55f, 60f, 200f, 900f).Level);
    }

    [Theory]
    [InlineData(null, 60f, 400f, 300f)]
    [InlineData(80f, null, 400f, 300f)]
    [InlineData(80f, 60f, null, 300f)]
    public void Silent_without_the_inputs(
        float? speedKmh,
        float? nextLimitKmh,
        float? nextDistanceMeters,
        float? massTonnes)
    {
        var state = BrakeAdvisory.Evaluate(speedKmh, nextLimitKmh, nextDistanceMeters, massTonnes);
        Assert.Equal(BrakeAdvisoryLevel.None, state.Level);
        Assert.True(string.IsNullOrEmpty(state.Text));
    }

    [Fact]
    public void Unknown_mass_assumes_the_worst_case()
    {
        var unknown = BrakeAdvisory.Evaluate(86f, 60f, 439f, massTonnes: null);
        Assert.Equal(BrakeAdvisoryLevel.Critical, unknown.Level);
    }

    [Fact]
    public void Text_includes_eta_seconds_and_meters()
    {
        var state = BrakeAdvisory.Evaluate(86f, 60f, 437f, 900f);
        Assert.Contains("Brake 60 in", state.Text);
        Assert.Contains("s", state.Text);
        Assert.Contains("m", state.Text);
        Assert.True(state.EtaSeconds > 0);
    }

    [Fact]
    public void Behind_the_board_is_silent()
    {
        Assert.Equal(
            BrakeAdvisoryLevel.None,
            BrakeAdvisory.Evaluate(86f, 60f, -5f, 900f).Level);
    }

    [Fact]
    public void Time_to_board_shrinks_as_speed_rises()
    {
        var slow = BrakeAdvisory.TimeToBoardSeconds(40f, 800f);
        var fast = BrakeAdvisory.TimeToBoardSeconds(80f, 800f);
        Assert.True(fast < slow);
    }
}

public class BrakeAdvisoryGradeTests
{
    [Fact]
    public void Downgrade_accelerates_and_upgrade_retards()
    {
        // -2.1 % ≈ +0.206 m/s² downhill (Gemini A116 math).
        var down = BrakeAdvisory.GradeAccelerationMps2(-2.1f);
        Assert.InRange(down, 0.19f, 0.22f);
        Assert.True(BrakeAdvisory.GradeAccelerationMps2(2.1f) < 0f);
        Assert.Equal(0f, BrakeAdvisory.GradeAccelerationMps2(0f));
    }

    [Fact]
    public void Downgrade_needs_much_more_room_than_flat()
    {
        var flat = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, 0f);
        var down = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, -2.1f);
        Assert.True(down > flat * 2f, $"flat={flat:0} down={down:0}");
    }

    [Fact]
    public void Upgrade_needs_less_room_than_flat()
    {
        var flat = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, 0f);
        var up = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, 2.1f);
        Assert.True(up < flat);
    }

    [Fact]
    public void Flat_grade_matches_the_no_grade_overload()
    {
        Assert.Equal(
            BrakeAdvisory.RequiredDistanceMeters(80f, 60f, 300f),
            BrakeAdvisory.RequiredDistanceMeters(80f, 60f, 300f, 0f));
    }

    [Fact]
    public void Soft_planning_distance_stays_finite_on_a_steep_downgrade()
    {
        var down = BrakeAdvisory.RequiredDistanceMeters(80f, 40f, 900f, -6f);
        Assert.True(float.IsFinite(down), "soft lead must stay finite for adopt math");
    }

    [Fact]
    public void Retuned_hard_braking_is_honest_on_two_percent()
    {
        Assert.False(BrakeAdvisory.IsRunaway(38f, -2.1f));
        Assert.True(BrakeAdvisory.IsRunaway(900f, -2.1f));
        Assert.True(BrakeAdvisory.IsRunaway(38f, -2.3f));
    }

    [Fact]
    public void Light_loco_sixty_nine_to_thirty_on_two_percent_requires_very_early_hard_braking()
    {
        var seconds = BrakeAdvisory.HardRequiredTimeSeconds(69f, 30f, 38f, -2f);
        Assert.True(seconds > 180f, $"expected >180 s from 0.5.57 trace retune, got {seconds:0}");
    }

    [Fact]
    public void Steep_downgrade_with_heavy_consist_is_a_runaway()
    {
        Assert.True(BrakeAdvisory.IsRunaway(900f, -5f));
    }

    [Fact]
    public void Runaway_reports_its_own_level_and_text()
    {
        var state = BrakeAdvisory.Evaluate(70f, 40f, 500f, 900f, -5f);
        Assert.Equal(BrakeAdvisoryLevel.Runaway, state.Level);
        Assert.Contains("RUNAWAY", state.Text);
        Assert.Equal(40, state.TargetKmh);
    }

    [Fact]
    public void Runaway_is_silent_when_nothing_slower_is_ahead()
    {
        Assert.Equal(
            BrakeAdvisoryLevel.None,
            BrakeAdvisory.Evaluate(40f, 60f, 500f, 900f, -5f).Level);
    }

    [Fact]
    public void Downgrade_warns_earlier_than_flat_at_the_same_distance()
    {
        var flat = BrakeAdvisory.Evaluate(60f, 40f, 5000f, 900f, 0f);
        var down = BrakeAdvisory.Evaluate(60f, 40f, 5000f, 900f, -2.1f);
        Assert.True(
            (int)down.Level > (int)flat.Level,
            $"flat={flat.Level} down={down.Level} should escalate on a downgrade");
    }

    [Fact]
    public void Critical_means_hard_braking_is_now_required()
    {
        var eased = BrakeAdvisory.Evaluate(86f, 60f, 2000f, 900f, 0f);
        var tight = BrakeAdvisory.Evaluate(86f, 60f, 300f, 900f, 0f);
        Assert.Equal(BrakeAdvisoryLevel.Advisory, eased.Level);
        Assert.Equal(BrakeAdvisoryLevel.Critical, tight.Level);
    }

    [Fact]
    public void Brake_target_prefers_the_tightest_drop_and_ignores_looser_boards()
    {
        var nextDrop = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[]
            {
                new AheadBoard(60f, 400f),
                new AheadBoard(30f, 1500f),
            },
            speedKmh: 70f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(nextDrop);
        Assert.Equal(30f, nextDrop!.Value.Kmh);
        Assert.Equal(1500f, nextDrop.Value.AlongMeters);

        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(80f, 600f) },
            speedKmh: 70f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2"));
    }

    [Fact]
    public void Posted_limit_brake_targets_the_next_slower_board()
    {
        var target = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(60f, 900f) },
            speedKmh: 80f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(target);
        Assert.Equal(60f, target!.Value.Kmh);
        Assert.Equal(900f, target.Value.AlongMeters);
    }

    /// <summary>
    /// 0.5.59 FAIL (Player.log 1815): the moment Limit adopted 30 the Brake chip went
    /// <c>adv=None</c>, so the only actionable warning vanished exactly when it was needed.
    /// The target depends on our <b>speed</b>, never on what the Limit chip already shows.
    /// </summary>
    [Fact]
    public void Brake_target_survives_the_limit_chip_adopting_the_same_number()
    {
        var target = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, 1500f) },
            speedKmh: 70f,
            massTonnes: 37.8f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(target);
        Assert.Equal(30f, target!.Value.Kmh);
    }

    [Fact]
    public void Brake_target_is_silent_once_we_are_slow_enough_for_the_board()
    {
        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, 400f) },
            speedKmh: 31f,
            massTonnes: 37.8f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2"));
    }

    /// <summary>
    /// A116 deep-dive, issue #2: cruising at 44 with a 40 board (Limit chip shows yellow, not red —
    /// <see cref="SpeedLimitDisplay.NearAboveKmh"/> = 5) must not also raise "Brake 40 in Xs". The
    /// two chips must agree: no Limit red, no Brake either.
    /// </summary>
    [Fact]
    public void Brake_target_matches_the_limit_chips_own_yellow_tolerance()
    {
        Assert.Equal(SpeedLimitDisplay.NearAboveKmh, BrakeAdvisory.MinTargetDeltaKmh);

        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(40f, 200f) },
            speedKmh: 44f,
            massTonnes: 37.8f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2"));

        var overTheYellowBand = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(40f, 200f) },
            speedKmh: 46f,
            massTonnes: 37.8f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(overTheYellowBand);
        Assert.Equal(40f, overTheYellowBand!.Value.Kmh);
    }

    [Fact]
    public void Early_target_sees_far_thirty_past_near_sixty()
    {
        // Sit inside the planning window (25% margin as of 0.5.65) but behind a nearer 60.
        var window = BrakeAdvisory.PlanningDistanceMeters(70f, 30f, 38f, 0f, "LocoDE2");
        var farThirty = window - 100f;
        var target = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[]
            {
                new AheadBoard(60f, 400f),
                new AheadBoard(30f, farThirty),
            },
            speedKmh: 70f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");

        Assert.NotNull(target);
        Assert.Equal(30f, target!.Value.Kmh);
        Assert.Equal(farThirty, target.Value.AlongMeters);
    }

    [Fact]
    public void Early_target_waits_until_planning_margin_window()
    {
        var window = BrakeAdvisory.PlanningDistanceMeters(70f, 30f, 38f, 0f, "LocoDE2");

        var outside = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, window + 100f) },
            speedKmh: 70f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");
        var inside = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, window - 100f) },
            speedKmh: 70f,
            massTonnes: 38f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2");

        Assert.Null(outside);
        Assert.NotNull(inside);
    }

    /// <summary>
    /// 0.5.59 FAIL (Player.log 1193–1215): <c>adv=Advisory 50</c> toggled to <c>adv=None</c> on
    /// consecutive frames while Limit stayed 60 — grade wobble moved the window edge across a
    /// board sitting on it. Once shown, a target holds until it is passed.
    /// </summary>
    [Fact]
    public void Latched_target_does_not_drop_out_at_the_window_edge()
    {
        var window = BrakeAdvisory.PlanningDistanceMeters(60f, 50f, 37.8f, 1.4f, "LocoDE2");
        var justOutside = window + 20f;

        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(50f, justOutside) },
            speedKmh: 60f,
            massTonnes: 37.8f,
            gradePercent: 1.4f,
            locomotiveTypeId: "LocoDE2"));

        var latched = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(50f, justOutside) },
            speedKmh: 60f,
            massTonnes: 37.8f,
            gradePercent: 1.4f,
            locomotiveTypeId: "LocoDE2",
            latchedTargetKmh: 50f);
        Assert.NotNull(latched);
        Assert.Equal(50f, latched!.Value.Kmh);
    }

    [Fact]
    public void Latch_does_not_widen_the_window_for_a_different_board()
    {
        var window = BrakeAdvisory.PlanningDistanceMeters(60f, 40f, 37.8f, 0f, "LocoDE2");
        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(40f, window + 200f) },
            speedKmh: 60f,
            massTonnes: 37.8f,
            gradePercent: 0f,
            locomotiveTypeId: "LocoDE2",
            latchedTargetKmh: 50f));
    }

    [Fact]
    public void Warning_lookahead_covers_seventy_to_thirty_with_margin()
    {
        var lookahead = BrakeAdvisory.WarningLookaheadMeters(
            70f, 38f, 0f, "LocoDE2");
        var required = BrakeAdvisory.EstimatedSlowdownTimeSeconds(
            70f, 30f, 38f, 0f, "LocoDE2");
        var requiredWarningDistance =
            SpeedDisplay.ToMetersPerSecond(70f)
            * required
            * BrakeAdvisory.WarningTimeMarginFactor;

        Assert.True(lookahead >= requiredWarningDistance - 1f);
        // 25% buffer (0.5.65) still covers well over a kilometre on flat DE2 70→30.
        Assert.True(lookahead > 1500f, $"expected >1.5 km, got {lookahead:0}");
    }

    [Fact]
    public void Unknown_loco_type_uses_conservative_slowdown_profile()
    {
        var de2 = BrakeAdvisory.EstimatedSlowdownTimeSeconds(
            70f, 30f, 38f, 0f, "LocoDE2");
        var unknown = BrakeAdvisory.EstimatedSlowdownTimeSeconds(
            70f, 30f, 38f, 0f, "LocoMystery");
        Assert.True(unknown > de2);
    }
}

/// <summary>
/// The warning window a player can rely on without memorising the route: the room a heavy
/// application actually needs, plus the planning margin (<see cref="BrakeAdvisory.WarningTimeMarginFactor"/>).
/// </summary>
public class BrakePlanningDistanceTests
{
    [Fact]
    public void Guaranteed_stop_distance_stays_finite_when_the_grade_beats_the_brakes()
    {
        var guaranteed = BrakeAdvisory.GuaranteedStopDistanceMeters(70f, 30f, 37.8f, -2.6f, "LocoDE2");
        Assert.True(float.IsFinite(guaranteed), "a runaway grade must still quote a planning distance");
        Assert.True(guaranteed > 3000f, $"expected kilometres of room, got {guaranteed:0} m");
    }

    [Fact]
    public void Guaranteed_stop_distance_grows_with_the_descent()
    {
        var flat = BrakeAdvisory.GuaranteedStopDistanceMeters(70f, 30f, 37.8f, 0f, "LocoDE2");
        var down = BrakeAdvisory.GuaranteedStopDistanceMeters(70f, 30f, 37.8f, -2f, "LocoDE2");
        Assert.True(down > flat * 2f, $"flat={flat:0} down={down:0}");
    }

    [Fact]
    public void Planning_distance_adds_fifty_percent_to_the_worst_case_room()
    {
        const float speed = 70f;
        const float target = 30f;
        const float mass = 37.8f;
        const float grade = -1f;
        var guaranteed = BrakeAdvisory.GuaranteedStopDistanceMeters(
            speed, target, mass, grade, "LocoDE2");
        var comfortable = SpeedDisplay.ToMetersPerSecond(speed)
                          * BrakeAdvisory.EstimatedSlowdownTimeSeconds(
                              speed, target, mass, grade, "LocoDE2");
        var worst = Math.Max(guaranteed, comfortable);

        var planning = BrakeAdvisory.PlanningDistanceMeters(
            speed, target, mass, grade, "LocoDE2");
        Assert.Equal(worst * BrakeAdvisory.WarningTimeMarginFactor, planning, 1f);
        Assert.True(planning > worst, "the buffer must be room we do not need");
    }

    [Theory]
    [InlineData(50f)]
    [InlineData(60f)]
    [InlineData(70f)]
    public void Cruise_speeds_warn_at_least_half_a_slowdown_before_the_room_runs_out(float speedKmh)
    {
        var guaranteed = BrakeAdvisory.GuaranteedStopDistanceMeters(
            speedKmh, 30f, 37.8f, -1.5f, "LocoDE2");
        var planning = BrakeAdvisory.PlanningDistanceMeters(
            speedKmh, 30f, 37.8f, -1.5f, "LocoDE2");
        Assert.True(
            planning >= guaranteed * BrakeAdvisory.WarningTimeMarginFactor,
            $"{speedKmh:0} km/h: planning={planning:0} guaranteed={guaranteed:0}");
    }

    [Fact]
    public void Warning_lookahead_never_falls_short_of_the_planning_window()
    {
        var planning = BrakeAdvisory.PlanningDistanceMeters(
            70f, BrakeAdvisory.WarningLookaheadTargetKmh, 37.8f, -1f, "LocoDE2");
        var lookahead = BrakeAdvisory.WarningLookaheadMeters(70f, 37.8f, -1f, "LocoDE2");
        Assert.Equal(Math.Min(planning, BrakeAdvisory.MaxWarningLookaheadMeters), lookahead, 1f);
    }

    [Fact]
    public void SelectEarlyTarget_ignores_boards_beyond_max_warning_lookahead()
    {
        var beyond = BrakeAdvisory.MaxWarningLookaheadMeters + 500f;
        Assert.Null(BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, beyond) },
            speedKmh: 70f,
            massTonnes: 37.8f,
            gradePercent: -2f,
            locomotiveTypeId: "LocoDE2"));

        // Well inside both planning and the 4.5 km cap (0.5.59 live case was ~1780 m).
        var hit = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[] { new AheadBoard(30f, 1780f) },
            speedKmh: 70f,
            massTonnes: 37.8f,
            gradePercent: -2.6f,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(hit);
        Assert.Equal(30f, hit!.Value.Kmh);
    }

    /// <summary>
    /// The 0.5.59 live failure (Player.log 1814–1815): DE2, 37.8 t, −2.6 %, 70 km/h, a 30 board
    /// 1 780 m out. The chip must be up at that range and must say the brakes will not hold.
    /// </summary>
    [Fact]
    public void Light_de2_at_seventy_warns_about_a_thirty_board_almost_two_kilometres_out()
    {
        const float speed = 70f;
        const float mass = 37.8f;
        const float grade = -2.6f;
        var target = BrakeAdvisory.SelectEarlyTarget(
            aheadBoards: new[]
            {
                new AheadBoard(70f, 450f),
                new AheadBoard(30f, 1780f),
            },
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: grade,
            locomotiveTypeId: "LocoDE2");
        Assert.NotNull(target);
        Assert.Equal(30f, target!.Value.Kmh);

        var state = BrakeAdvisory.Evaluate(
            speed, target.Value.Kmh, target.Value.AlongMeters, mass, grade, "LocoDE2");
        Assert.Equal(BrakeAdvisoryLevel.Runaway, state.Level);
        Assert.Contains("RUNAWAY", state.Text);
    }

    [Fact]
    public void Far_side_of_the_window_is_still_quiet_on_easy_ground()
    {
        var state = BrakeAdvisory.Evaluate(70f, 30f, 12000f, 37.8f, 0f, "LocoDE2");
        Assert.Equal(BrakeAdvisoryLevel.None, state.Level);
    }
}

/// <summary>
/// A board can drop out of one scan frame (path/lateral jitter). The warning must ride through
/// that instead of blinking off, so it closes the distance on its own for a moment.
/// </summary>
public class BrakeTargetCoastTests
{
    [Fact]
    public void Missing_board_keeps_its_target_and_closes_the_distance()
    {
        var coasted = BrakeAdvisory.CoastTarget(30f, 1000f, 0.5f, 72f);
        Assert.NotNull(coasted);
        Assert.Equal(30f, coasted!.Value.Kmh);
        Assert.InRange(coasted.Value.AlongMeters, 985f, 995f);
    }

    [Fact]
    public void Coast_gives_up_after_the_grace_window()
    {
        Assert.Null(BrakeAdvisory.CoastTarget(
            30f, 1000f, BrakeAdvisory.MaxCoastSeconds + 0.1f, 72f));
    }

    [Fact]
    public void Coast_stops_once_we_are_slow_enough()
    {
        Assert.Null(BrakeAdvisory.CoastTarget(30f, 1000f, 0.5f, 31f));
    }

    [Fact]
    public void Coast_stops_when_the_board_is_behind_us()
    {
        Assert.Null(BrakeAdvisory.CoastTarget(30f, 5f, 1.5f, 72f));
    }

    [Fact]
    public void Nothing_to_coast_without_a_previous_target()
    {
        Assert.Null(BrakeAdvisory.CoastTarget(null, null, 0.2f, 72f));
    }
}

public class AdverseGradeHoldTests
{
    [Fact]
    public void First_reading_is_taken_as_is()
    {
        Assert.Equal(-1.2f, AdverseGradeHold.Step(null, -1.2f, 0.1f));
    }

    [Fact]
    public void A_steeper_descent_applies_immediately()
    {
        Assert.Equal(-2.6f, AdverseGradeHold.Step(-0.3f, -2.6f, 0.02f));
    }

    /// <summary>
    /// Player.log shows grade jumping −2.6 → −0.3 → 0.1 across three frames. Planning must not
    /// follow that up: the friendlier reading has to be earned over time.
    /// </summary>
    [Fact]
    public void A_friendlier_reading_recovers_at_a_bounded_rate()
    {
        var held = AdverseGradeHold.Step(-2.6f, -0.3f, 0.02f);
        Assert.True(held < -2.5f, $"one frame must not erase the descent, got {held:0.00}");

        var later = AdverseGradeHold.Step(-2.6f, -0.3f, 1f);
        Assert.Equal(-2.6f + AdverseGradeHold.RecoveryPercentPerSecond, later, 0.001f);
    }

    [Fact]
    public void Recovery_never_overshoots_the_current_reading()
    {
        Assert.Equal(0.1f, AdverseGradeHold.Step(-2.6f, 0.1f, 60f));
    }
}

public class RecommendedSpeedLimitGradeTests
{
    [Fact]
    public void Downgrade_adopts_a_slower_board_that_flat_would_still_ignore()
    {
        // Place the board between flat adopt lead and descent adopt lead (dial-aware).
        var flatLead = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, 0f)
            * RecommendedSpeedLimit.AdoptLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        var downLead = BrakeAdvisory.RequiredDistanceMeters(60f, 40f, 900f, -2.1f)
            * RecommendedSpeedLimit.AdoptLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        Assert.True(downLead > flatLead + 5f, "descent must widen adopt lead vs flat");
        var along = (flatLead + downLead) * 0.5f;
        var boards = new[] { new AheadBoard(40f, along) };
        var flat = RecommendedSpeedLimit.Resolve(
            postedKmh: 60f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: 60f,
            massTonnes: 900f,
            gradePercent: 0f,
            adoptedAlongMeters: out _);
        var down = RecommendedSpeedLimit.Resolve(
            postedKmh: 60f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: 60f,
            massTonnes: 900f,
            gradePercent: -2.1f,
            adoptedAlongMeters: out var downAlong);
        Assert.Equal(60f, flat);
        Assert.Equal(40f, down);
        Assert.Equal(along, downAlong);
    }

    [Fact]
    public void Sticky_adopt_holds_far_restriction_across_grade_wobble()
    {
        // Player.log 30↔60: far 30 sits on the soft-lead edge; grade −0.6↔−0.7 flips adopt.
        const float speed = 70f;
        const float mass = 38f;
        const float mild = -0.6f;
        const float steeper = -0.7f;
        var adoptMild = BrakeAdvisory.RequiredDistanceMeters(speed, 30f, mass, mild)
            * RecommendedSpeedLimit.AdoptLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        var adoptSteep = BrakeAdvisory.RequiredDistanceMeters(speed, 30f, mass, steeper)
            * RecommendedSpeedLimit.AdoptLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        Assert.True(adoptSteep > adoptMild + 2f, "grades must separate the adopt edge");
        var far30 = (adoptMild + adoptSteep) * 0.5f;
        var near60 = Math.Max(
            40f,
            BrakeAdvisory.RequiredDistanceMeters(speed, 60f, mass, mild)
                * RecommendedSpeedLimit.AdoptLeadFactor
                * SpeedLimitAggressiveness.LimitLeadScale
                * 0.5f);
        var boards = new[]
        {
            new AheadBoard(60f, near60),
            new AheadBoard(30f, far30),
        };

        var atSteep = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: steeper,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: null);
        var atMildFresh = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: mild,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: null);
        var atMildSticky = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: mild,
            adoptedAlongMeters: out var stickyAlong,
            stickyAdoptedKmh: 30f);

        Assert.Equal(30f, atSteep);
        Assert.Equal(60f, atMildFresh);
        Assert.Equal(30f, atMildSticky);
        Assert.Equal(far30, stickyAlong);
    }

    [Fact]
    public void Sticky_adopt_releases_when_clearly_outside_release_lead()
    {
        const float speed = 70f;
        const float mass = 38f;
        const float grade = -0.6f;
        var release = BrakeAdvisory.RequiredDistanceMeters(speed, 30f, mass, grade)
            * RecommendedSpeedLimit.ReleaseLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        var near60 = Math.Max(
            40f,
            BrakeAdvisory.RequiredDistanceMeters(speed, 60f, mass, grade)
                * RecommendedSpeedLimit.AdoptLeadFactor
                * SpeedLimitAggressiveness.LimitLeadScale
                * 0.5f);
        var boards = new[]
        {
            new AheadBoard(60f, near60),
            new AheadBoard(30f, release + 100f),
        };

        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: grade,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: 30f);
        Assert.Equal(60f, recommended);
    }

    [Fact]
    public void Sticky_adopt_does_not_release_when_speed_drops_toward_board()
    {
        // Player.log 50↔60: braking shrinks soft lead; sticky must not un-adopt mid-slowdown.
        const float mass = 38f;
        const float grade = 1.2f;
        const float fast = 69f;
        const float slowed = 58f;
        var releaseAtSlow = BrakeAdvisory.RequiredDistanceMeters(slowed, 50f, mass, grade)
            * RecommendedSpeedLimit.ReleaseLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        var releaseFloor = BrakeAdvisory.RequiredDistanceMeters(
                Math.Max(slowed, 50f + RecommendedSpeedLimit.StickyReleaseSpeedMarginKmh),
                50f,
                mass,
                grade)
            * RecommendedSpeedLimit.ReleaseLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        Assert.True(releaseFloor > releaseAtSlow + 2f, "speed floor must widen release vs slowed");
        var along = (releaseAtSlow + releaseFloor) * 0.5f;
        var near60 = Math.Max(
            40f,
            BrakeAdvisory.RequiredDistanceMeters(slowed, 60f, mass, grade)
                * RecommendedSpeedLimit.AdoptLeadFactor
                * SpeedLimitAggressiveness.LimitLeadScale
                * 0.5f);
        var boards = new[]
        {
            new AheadBoard(60f, near60),
            new AheadBoard(50f, along),
        };

        var atFast = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: fast,
            massTonnes: mass,
            gradePercent: grade,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: 50f);
        var atSlowFresh = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: slowed,
            massTonnes: mass,
            gradePercent: grade,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: null);
        var atSlowSticky = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: slowed,
            massTonnes: mass,
            gradePercent: grade,
            adoptedAlongMeters: out var stickyAlong,
            stickyAdoptedKmh: 50f);

        Assert.Equal(50f, atFast);
        Assert.Equal(60f, atSlowFresh);
        Assert.Equal(50f, atSlowSticky);
        Assert.Equal(along, stickyAlong);
    }

    [Fact]
    public void Sticky_adopt_survives_brief_scan_drop()
    {
        // Board briefly leaves AheadBoards (path/lateral jitter) — keep sticky Restriction.
        var withoutFifty = new[] { new AheadBoard(60f, 300f) };
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: withoutFifty,
            geometryKmh: null,
            speedKmh: 69f,
            massTonnes: 38f,
            gradePercent: 1.2f,
            adoptedAlongMeters: out var along,
            stickyAdoptedKmh: 50f);
        Assert.Equal(50f, recommended);
        Assert.NotNull(along);
    }

    [Fact]
    public void Sticky_adopt_does_not_release_when_grade_eases()
    {
        // Player.log 30↔60: adopt on descent; grade −1.0→+0.2 must not shrink release lead.
        const float speed = 71f;
        const float mass = 38f;
        const float adoptGrade = -1.0f;
        const float easedGrade = 0.2f;
        var releaseAtAdopt = BrakeAdvisory.RequiredDistanceMeters(
                Math.Max(speed, 30f + RecommendedSpeedLimit.StickyReleaseSpeedMarginKmh),
                30f,
                mass,
                adoptGrade)
            * RecommendedSpeedLimit.ReleaseLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        var releaseEased = BrakeAdvisory.RequiredDistanceMeters(
                Math.Max(speed, 30f + RecommendedSpeedLimit.StickyReleaseSpeedMarginKmh),
                30f,
                mass,
                easedGrade)
            * RecommendedSpeedLimit.ReleaseLeadFactor
            * SpeedLimitAggressiveness.LimitLeadScale;
        Assert.True(releaseAtAdopt > releaseEased + 2f, "descent must widen release vs eased");
        var along = (releaseAtAdopt + releaseEased) * 0.5f;
        var near60 = Math.Max(
            40f,
            BrakeAdvisory.RequiredDistanceMeters(speed, 60f, mass, easedGrade)
                * RecommendedSpeedLimit.AdoptLeadFactor
                * SpeedLimitAggressiveness.LimitLeadScale
                * 0.5f);
        var boards = new[]
        {
            new AheadBoard(60f, near60),
            new AheadBoard(30f, along),
        };

        var easedFresh = RecommendedSpeedLimit.Resolve(
            postedKmh: 70f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: easedGrade,
            adoptedAlongMeters: out _,
            stickyAdoptedKmh: null);
        var easedSticky = RecommendedSpeedLimit.Resolve(
            postedKmh: 70f,
            aheadBoards: boards,
            geometryKmh: null,
            speedKmh: speed,
            massTonnes: mass,
            gradePercent: easedGrade,
            adoptedAlongMeters: out var stickyAlong,
            stickyAdoptedKmh: 30f,
            stickyAdoptGradePercent: adoptGrade);

        Assert.Equal(60f, easedFresh);
        Assert.Equal(30f, easedSticky);
        Assert.Equal(along, stickyAlong);
    }
}

public class StickyAdoptedLimitTests
{
    [Fact]
    public void Looser_adopt_does_not_clobber_tighter_sticky()
    {
        var next = StickyAdoptedLimit.Step(
            previousKmh: 30f,
            previousGradePercent: -1f,
            adoptedKmh: 60f,
            postedKmh: 70f,
            gradePercent: 0.2f);
        Assert.Equal(30f, next.Kmh);
        Assert.Equal(-1f, next.GradePercent);
    }

    [Fact]
    public void Null_adopt_keeps_sticky_until_posted_takes_it()
    {
        var keep = StickyAdoptedLimit.Step(
            previousKmh: 30f,
            previousGradePercent: -1f,
            adoptedKmh: null,
            postedKmh: 70f,
            gradePercent: 0f);
        Assert.Equal(30f, keep.Kmh);

        var taken = StickyAdoptedLimit.Step(
            previousKmh: 30f,
            previousGradePercent: -1f,
            adoptedKmh: null,
            postedKmh: 30f,
            gradePercent: 0f);
        Assert.Null(taken.Kmh);
    }
}

public class RecommendedSpeedLimitTests
{
    [Fact]
    public void Adopts_slower_board_inside_soft_lead_not_at_the_tire()
    {
        var lead = BrakeAdvisory.RequiredDistanceMeters(80f, 60f, 38f, 0f)
                   * RecommendedSpeedLimit.AdoptLeadFactor
                   * SpeedLimitAggressiveness.LimitLeadScale;
        var alongBoard = Math.Max(50f, lead * 0.5f);
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: new[] { new AheadBoard(60f, alongBoard) },
            geometryKmh: null,
            speedKmh: 80f,
            massTonnes: 38f,
            out var along);
        Assert.Equal(60f, recommended);
        Assert.Equal(alongBoard, along);
    }

    [Fact]
    public void Does_not_adopt_when_still_far_outside_soft_lead()
    {
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: new[] { new AheadBoard(60f, 5000f) },
            geometryKmh: null,
            speedKmh: 80f,
            massTonnes: 38f,
            out _);
        Assert.Equal(80f, recommended);
    }

    [Fact]
    public void Intermediate_board_does_not_hide_a_tighter_drop()
    {
        var along60 = Math.Max(
            80f,
            BrakeAdvisory.RequiredDistanceMeters(81f, 60f, 38f, 0f)
                * RecommendedSpeedLimit.AdoptLeadFactor
                * SpeedLimitAggressiveness.LimitLeadScale
                * 0.5f);
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 90f,
            aheadBoards: new[]
            {
                new AheadBoard(80f, 50f),
                new AheadBoard(60f, along60),
            },
            geometryKmh: null,
            speedKmh: 81f,
            massTonnes: 38f,
            out var along);
        Assert.Equal(60f, recommended);
        Assert.Equal(along60, along);
    }

    [Fact]
    public void Keeps_upcoming_board_when_already_at_that_speed()
    {
        // Hold-ahead is dial-scaled; keep the board inside MinHold / time hold.
        var hold = Math.Max(
            SpeedLimitAggressiveness.MinHoldAheadMeters,
            SpeedDisplay.ToMetersPerSecond(60f) * SpeedLimitAggressiveness.HoldAheadSeconds)
            * SpeedLimitAggressiveness.LimitLeadScale;
        var along = Math.Max(20f, hold * 0.5f);
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: new[] { new AheadBoard(60f, along) },
            geometryKmh: null,
            speedKmh: 60f,
            massTonnes: 38f,
            out _);
        Assert.Equal(60f, recommended);
    }

    [Fact]
    public void Geometry_does_not_override_posted_board()
    {
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 90f,
            aheadBoards: new[] { new AheadBoard(80f, 5000f) },
            geometryKmh: 40f,
            speedKmh: 42f,
            massTonnes: 36f,
            out _);
        Assert.Equal(90f, recommended);
    }

    [Fact]
    public void Geometry_alone_when_no_boards()
    {
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: null,
            aheadBoards: null,
            geometryKmh: 70f,
            speedKmh: 60f,
            massTonnes: 38f,
            out _);
        Assert.Equal(70f, recommended);
    }

    [Fact]
    public void NextDifferent_skips_same_number()
    {
        var next = RecommendedSpeedLimit.NextDifferent(
            60f,
            new[]
            {
                new AheadBoard(60f, 100f),
                new AheadBoard(50f, 300f),
            });
        Assert.NotNull(next);
        Assert.Equal(50f, next!.Value.Kmh);
        Assert.Equal(300f, next.Value.AlongMeters);
    }
}
