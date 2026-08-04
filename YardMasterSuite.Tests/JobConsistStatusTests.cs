using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobConsistStatusEvalTests
{
    [Theory]
    [InlineData(3, 0, 0, JobConsistStatus.Missing)]
    [InlineData(3, 0, 2, JobConsistStatus.Missing)]
    [InlineData(0, 0, 0, JobConsistStatus.Missing)]
    [InlineData(3, 3, 0, JobConsistStatus.Ready)]
    [InlineData(3, 3, 1, JobConsistStatus.Hold)]
    [InlineData(3, 2, 0, JobConsistStatus.Hold)]
    [InlineData(3, 1, 1, JobConsistStatus.Hold)]
    public void Evaluate_maps_counts(
        int expected,
        int attached,
        int foreign,
        JobConsistStatus want)
    {
        Assert.Equal(want, JobConsistStatusEval.Evaluate(expected, attached, foreign));
    }
}

public class JobConsistStatusDisplayTests
{
    [Fact]
    public void Format_plain_and_hud_colors()
    {
        Assert.Equal("GO", JobConsistStatusDisplay.Format(JobConsistStatus.Ready));
        Assert.Equal("HOLD", JobConsistStatusDisplay.Format(JobConsistStatus.Hold));
        Assert.Equal("RED", JobConsistStatusDisplay.Format(JobConsistStatus.Missing));

        Assert.Contains(JobConsistStatusDisplay.GoColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Ready));
        Assert.Contains(JobConsistStatusDisplay.HoldColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Hold));
        Assert.Contains(JobConsistStatusDisplay.RedColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Missing));
        Assert.Contains("GO", JobConsistStatusDisplay.FormatHud(JobConsistStatus.Ready));
    }
}
