using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 2026-08-07 smoke: rhythmic ~2.5 s hitch standing still, driving, in and out of town, gone with
/// the mod disabled. AR cache probes must not run once per marker per frame.
/// </summary>
public class PerFrameProbeThrottleTests
{
    [Fact]
    public void Smoke_hitch_marker_redraw_does_not_reprobe_every_frame()
    {
        const float oneFrameAt60Fps = 1f / 60f;
        Assert.False(PerFrameProbeThrottle.Due(oneFrameAt60Fps, PerFrameProbeThrottle.JobIdentitySeconds));
        Assert.False(PerFrameProbeThrottle.Due(oneFrameAt60Fps, PerFrameProbeThrottle.TownProximitySeconds));
    }

    [Fact]
    public void Job_pickup_shows_within_a_quarter_second()
    {
        Assert.True(PerFrameProbeThrottle.Due(0.25f, PerFrameProbeThrottle.JobIdentitySeconds));
    }

    [Fact]
    public void Town_entry_probes_twice_a_second()
    {
        Assert.False(PerFrameProbeThrottle.Due(0.49f, PerFrameProbeThrottle.TownProximitySeconds));
        Assert.True(PerFrameProbeThrottle.Due(0.5f, PerFrameProbeThrottle.TownProximitySeconds));
    }
}
