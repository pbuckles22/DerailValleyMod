using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HitchCadenceProbeTests
{
    [Fact]
    public void NextSpikeMessage_silent_under_threshold()
    {
        Assert.Null(HitchCadenceProbe.NextSpikeMessage(1.02f, 1f, out var next));
        Assert.Equal(1.02f, next);
    }

    [Fact]
    public void NextSpikeMessage_logs_when_over_threshold()
    {
        var msg = HitchCadenceProbe.NextSpikeMessage(1.05f, 1f, out _);
        Assert.Equal("T2 hitch-spike: dt=50ms", msg);
    }

    [Fact]
    public void NextSpikeMessage_throttles_log_spam()
    {
        var msg = HitchCadenceProbe.NextSpikeMessage(
            2f,
            1.9f,
            lastLogAt: 1.5f,
            out _,
            out var nextLog);
        Assert.Null(msg);
        Assert.Equal(1.5f, nextLog);

        msg = HitchCadenceProbe.NextSpikeMessage(
            3f,
            2.9f,
            lastLogAt: 1.5f,
            out _,
            out nextLog);
        Assert.Equal("T2 hitch-spike: dt=100ms", msg);
        Assert.Equal(3f, nextLog);
    }
}
