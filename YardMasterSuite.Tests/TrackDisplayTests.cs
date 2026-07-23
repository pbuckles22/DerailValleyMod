using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TrackDisplayTests
{
    [Fact]
    public void Format_omits_segment_when_missing_or_blank()
    {
        Assert.Null(TrackDisplay.Format(null));
        Assert.Null(TrackDisplay.Format(""));
        Assert.Null(TrackDisplay.Format("   "));
    }

    [Fact]
    public void Format_shows_track_id()
    {
        Assert.Equal("Track SM-O6I", TrackDisplay.Format("SM-O6I"));
        Assert.Equal("Track C-06S", TrackDisplay.Format(" C-06S "));
    }
}
