using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BrakeLimitAlignTests
{
    [Fact]
    public void Brake_thirty_tightens_posted_sixty_recommendation()
    {
        var applied = BrakeLimitAlign.TryApply(
            recommendedKmh: 60f,
            recommendedAlongMeters: null,
            brakeTarget: new AheadBoard(30f, 2688f, fromGeometry: true),
            out var aligned,
            out var along);

        Assert.True(applied);
        Assert.Equal(30f, aligned);
        Assert.Equal(2688f, along);
    }

    [Fact]
    public void No_brake_target_leaves_recommendation_alone()
    {
        var applied = BrakeLimitAlign.TryApply(
            recommendedKmh: 60f,
            recommendedAlongMeters: 100f,
            brakeTarget: null,
            out var aligned,
            out var along);

        Assert.False(applied);
        Assert.Equal(60f, aligned);
        Assert.Equal(100f, along);
    }

    [Fact]
    public void Looser_brake_target_does_not_raise_a_tighter_recommendation()
    {
        var applied = BrakeLimitAlign.TryApply(
            recommendedKmh: 30f,
            recommendedAlongMeters: 500f,
            brakeTarget: new AheadBoard(40f, 200f),
            out var aligned,
            out var along);

        Assert.False(applied);
        Assert.Equal(30f, aligned);
        Assert.Equal(500f, along);
    }
}
