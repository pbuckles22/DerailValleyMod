using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobDisplayTests
{
    [Fact]
    public void Format_omits_segment_when_missing_or_blank()
    {
        Assert.Null(JobDisplay.Format(null));
        Assert.Null(JobDisplay.Format(""));
        Assert.Null(JobDisplay.Format("   "));
    }

    [Fact]
    public void Format_shows_job_id()
    {
        Assert.Equal("Job FH-123", JobDisplay.Format("FH-123"));
        Assert.Equal("Job SM-SU-46", JobDisplay.Format(" SM-SU-46 "));
    }
}
