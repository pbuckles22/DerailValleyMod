using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_missing()
    {
        Assert.Equal("— Job", JobDisplay.Format(null));
        Assert.Equal("— Job", JobDisplay.Format(""));
        Assert.Equal("— Job", JobDisplay.Format("   "));
    }

    [Fact]
    public void Format_shows_job_id()
    {
        Assert.Equal("Job FH-123", JobDisplay.Format("FH-123"));
    }
}
