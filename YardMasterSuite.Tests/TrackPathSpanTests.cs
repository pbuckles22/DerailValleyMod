using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class TrackPathSpanTests
{
    [Fact]
    public void Within_track_grows_with_span_when_traveling_in_to_out()
    {
        Assert.Equal(0f, TrackPathSpan.WithinTrackMeters(0f, 100f, travelIncreasingSpan: true));
        Assert.Equal(40f, TrackPathSpan.WithinTrackMeters(40f, 100f, travelIncreasingSpan: true));
        Assert.Equal(100f, TrackPathSpan.WithinTrackMeters(100f, 100f, travelIncreasingSpan: true));
    }

    [Fact]
    public void Within_track_flips_when_traveling_out_to_in()
    {
        Assert.Equal(100f, TrackPathSpan.WithinTrackMeters(0f, 100f, travelIncreasingSpan: false));
        Assert.Equal(60f, TrackPathSpan.WithinTrackMeters(40f, 100f, travelIncreasingSpan: false));
        Assert.Equal(0f, TrackPathSpan.WithinTrackMeters(100f, 100f, travelIncreasingSpan: false));
    }

    [Fact]
    public void Within_track_clamps_span_to_track_length()
    {
        Assert.Equal(0f, TrackPathSpan.WithinTrackMeters(-5f, 80f, travelIncreasingSpan: true));
        Assert.Equal(80f, TrackPathSpan.WithinTrackMeters(120f, 80f, travelIncreasingSpan: true));
        Assert.Equal(0f, TrackPathSpan.WithinTrackMeters(120f, 80f, travelIncreasingSpan: false));
    }
}
