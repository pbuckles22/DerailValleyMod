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
    /// A light loco can still ease 86→60 in 439 m, so yellow is correct there; red is reserved for
    /// "hard application required now", which arrives ~300 m out.
    /// </summary>
    [Fact]
    public void Light_loco_warns_at_439m_and_goes_red_when_hard_braking_is_needed()
    {
        Assert.Equal(BrakeAdvisoryLevel.Advisory, BrakeAdvisory.Evaluate(86f, 60f, 439f, 38f).Level);
        Assert.Equal(BrakeAdvisoryLevel.Critical, BrakeAdvisory.Evaluate(86f, 60f, 300f, 38f).Level);
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
    public void Hard_braking_still_holds_a_light_loco_on_two_percent()
    {
        Assert.False(BrakeAdvisory.IsRunaway(38f, -2.1f));
        Assert.False(BrakeAdvisory.IsRunaway(900f, -2.1f));
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
}

public class RecommendedSpeedLimitGradeTests
{
    [Fact]
    public void Downgrade_adopts_a_slower_board_that_flat_would_still_ignore()
    {
        var boards = new[] { new AheadBoard(40f, 1400f) };
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
        Assert.Equal(1400f, downAlong);
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
            * RecommendedSpeedLimit.AdoptLeadFactor;
        var adoptSteep = BrakeAdvisory.RequiredDistanceMeters(speed, 30f, mass, steeper)
            * RecommendedSpeedLimit.AdoptLeadFactor;
        Assert.True(adoptSteep > adoptMild + 50f, "grades must separate the adopt edge");
        var far30 = (adoptMild + adoptSteep) * 0.5f;
        var boards = new[]
        {
            new AheadBoard(60f, 500f),
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
            * RecommendedSpeedLimit.ReleaseLeadFactor;
        var boards = new[]
        {
            new AheadBoard(60f, 500f),
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
}

public class RecommendedSpeedLimitTests
{
    [Fact]
    public void Adopts_slower_board_inside_soft_lead_not_at_the_tire()
    {
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: new[] { new AheadBoard(60f, 800f) },
            geometryKmh: null,
            speedKmh: 80f,
            massTonnes: 38f,
            out var along);
        Assert.Equal(60f, recommended);
        Assert.Equal(800f, along);
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
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 90f,
            aheadBoards: new[]
            {
                new AheadBoard(80f, 50f),
                new AheadBoard(60f, 500f),
            },
            geometryKmh: null,
            speedKmh: 81f,
            massTonnes: 38f,
            out var along);
        Assert.Equal(60f, recommended);
        Assert.Equal(500f, along);
    }

    [Fact]
    public void Keeps_upcoming_board_when_already_at_that_speed()
    {
        var recommended = RecommendedSpeedLimit.Resolve(
            postedKmh: 80f,
            aheadBoards: new[] { new AheadBoard(60f, 144f) },
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
