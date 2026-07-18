using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HandbrakeDisplayTests
{
    [Fact]
    public void FormatCount_shows_placeholder_when_missing()
    {
        Assert.Equal("— Handbrake", HandbrakeDisplay.FormatCount(null));
    }

    [Theory]
    [InlineData(0, "Handbrake 0")]
    [InlineData(2, "Handbrake 2")]
    [InlineData(12, "Handbrake 12")]
    public void FormatCount_shows_applied_count(int applied, string expected)
    {
        Assert.Equal(expected, HandbrakeDisplay.FormatCount(applied));
    }

    [Fact]
    public void FormatTotal_shows_placeholder_when_missing()
    {
        Assert.Equal("— Handbrakes", HandbrakeDisplay.FormatTotal(null));
    }

    [Theory]
    [InlineData(0, "Handbrakes 0")]
    [InlineData(3, "Handbrakes 3")]
    public void FormatTotal_shows_consist_applied_count(int applied, string expected)
    {
        Assert.Equal(expected, HandbrakeDisplay.FormatTotal(applied));
    }

    [Fact]
    public void CountApplied_counts_positions_above_threshold()
    {
        var positions = new[] { 0f, 0.001f, 0.5f, 1f, 0.01f, 0.011f };
        Assert.Equal(3, HandbrakeDisplay.CountApplied(positions));
    }

    [Fact]
    public void IsApplied_false_at_or_below_threshold()
    {
        Assert.False(HandbrakeDisplay.IsApplied(0f));
        Assert.False(HandbrakeDisplay.IsApplied(HandbrakeDisplay.AppliedThreshold));
        Assert.True(HandbrakeDisplay.IsApplied(HandbrakeDisplay.AppliedThreshold + 0.001f));
    }
}
