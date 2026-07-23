using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TrackDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_missing()
    {
        Assert.Equal("— Track", TrackDisplay.Format(null));
        Assert.Equal("— Track", TrackDisplay.Format(""));
        Assert.Equal("— Track", TrackDisplay.Format("   "));
    }

    [Fact]
    public void Format_shows_track_id()
    {
        Assert.Equal("Track SM-O6I", TrackDisplay.Format("SM-O6I"));
        Assert.Equal("Track C-06S", TrackDisplay.Format(" C-06S "));
    }
}
