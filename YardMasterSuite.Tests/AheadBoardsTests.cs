using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AheadBoardsTests
{
    [Fact]
    public void NextDifferent_picks_nearest_different_number()
    {
        var boards = new[]
        {
            new AheadBoard(80f, 40f),
            new AheadBoard(50f, 120f),
            new AheadBoard(50f, 200f),
        };

        var next = AheadBoards.NextDifferent(80f, boards);
        Assert.NotNull(next);
        Assert.Equal(50f, next!.Value.Kmh);
        Assert.Equal(120f, next.Value.AlongMeters);
    }

    [Fact]
    public void NextDifferent_skips_same_posted_number()
    {
        var boards = new[]
        {
            new AheadBoard(80f, 30f),
            new AheadBoard(80f, 90f),
        };

        Assert.Null(AheadBoards.NextDifferent(80f, boards));
    }
}

public class WorldSpeedBoardIndexTests
{
    [Fact]
    public void Remember_survives_and_returns_by_track()
    {
        var index = new WorldSpeedBoardIndex();
        index.Remember(42, 50f, 100f, 2f, 200f, travelX: 1f, travelZ: 0f);
        index.Remember(42, 80f, 150f, 2f, 250f, travelX: 1f, travelZ: 0f);
        index.Remember(7, 40f, 0f, 0f, 0f, travelX: 0f, travelZ: 1f);

        Assert.Equal(2, index.ForTrack(42).Count);
        Assert.Single(index.ForTrack(7));
    }

    [Fact]
    public void SameTravel_rejects_opposite_direction()
    {
        var index = new WorldSpeedBoardIndex();
        index.Remember(1, 50f, 10f, 0f, 10f, travelX: 1f, travelZ: 0f);
        var pin = index.ForTrack(1)[0];
        Assert.True(WorldSpeedBoardIndex.SameTravel(pin, 1f, 0f));
        Assert.False(WorldSpeedBoardIndex.SameTravel(pin, -1f, 0f));
    }
}

public class NextLimitRevealTests
{
    [Fact]
    public void Reveal_for_60_to_40_is_hundreds_of_meters_not_kilometres()
    {
        var d = NextLimitReveal.RevealMeters(60f, 40f, massTonnes: 38f);
        Assert.InRange(d, 200f, 600f);
    }

    [Fact]
    public void Reveal_for_80_to_40_is_longer_than_60_to_40()
    {
        var mild = NextLimitReveal.RevealMeters(60f, 40f, 38f);
        var steep = NextLimitReveal.RevealMeters(80f, 40f, 38f);
        Assert.True(steep > mild);
        Assert.True(steep <= NextLimitReveal.MaxRevealMeters);
    }

    [Fact]
    public void ShowDistance_false_when_far()
    {
        Assert.False(NextLimitReveal.ShowDistance(800f, 70f, 50f, 38f));
        Assert.True(NextLimitReveal.ShowDistance(100f, 70f, 50f, 38f));
    }
}

public class SpeedLimitNextDisplayTests
{
    [Fact]
    public void Format_far_next_omits_meters()
    {
        Assert.Equal(
            "Limit 80 | Next 50",
            SpeedLimitDisplay.Format(80f, nextKmh: 50f, nextDistanceMeters: 800f, massTonnes: 38f));
    }

    [Fact]
    public void Format_close_next_includes_meters()
    {
        Assert.Equal(
            "Limit 80 | Next 50 (50m)",
            SpeedLimitDisplay.Format(80f, nextKmh: 50f, nextDistanceMeters: 50f, massTonnes: 38f));
    }

    [Fact]
    public void Format_has_no_posted_label()
    {
        Assert.DoesNotContain("Posted", SpeedLimitDisplay.Format(70f, nextKmh: 80f, nextDistanceMeters: 85f));
    }

    [Fact]
    public void FormatHud_colors_limit_chip_not_next()
    {
        var hud = SpeedLimitDisplay.FormatHud(
            speedKmh: 86f,
            limitKmh: 80f,
            nextKmh: 50f,
            nextDistanceMeters: 50f,
            massTonnes: 38f);
        Assert.StartsWith($"<color={SpeedLimitDisplay.CriticalColor}>Limit 80</color>", hud);
        Assert.Contains("Next 50 (50m)", hud);
    }
}
