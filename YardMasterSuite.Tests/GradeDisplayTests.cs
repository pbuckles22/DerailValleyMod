using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class GradeDisplayTests
{
    [Fact]
    public void PercentFromDirection_level_track_is_zero()
    {
        Assert.Equal(0f, GradeDisplay.PercentFromDirection(1f, 0f, 0f), precision: 2);
    }

    [Fact]
    public void PercentFromDirection_one_percent_climb()
    {
        // rise/run = 0.01 → 1%
        Assert.Equal(1f, GradeDisplay.PercentFromDirection(100f, 1f, 0f), precision: 2);
    }

    [Fact]
    public void PercentFromDirection_descent_is_negative()
    {
        Assert.Equal(-2f, GradeDisplay.PercentFromDirection(50f, -1f, 0f), precision: 2);
    }

    [Fact]
    public void FormatPercent_shows_sign_and_placeholder()
    {
        Assert.Equal("— Grade", GradeDisplay.FormatPercent(null));
        Assert.Equal("Grade +1.2 %", GradeDisplay.FormatPercent(1.24f));
        Assert.Equal("Grade -0.5 %", GradeDisplay.FormatPercent(-0.54f));
        Assert.Equal("Grade 0.0 %", GradeDisplay.FormatPercent(0.01f));
    }
}
