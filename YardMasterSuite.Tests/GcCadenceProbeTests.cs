using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Diagnostic for the ~2.5 s hitch: prove the cadence with gen-0 counts in Player.log instead of
/// eyeballing a video. One line per window, and only when a collection actually happened.
/// </summary>
public class GcCadenceProbeTests
{
    [Fact]
    public void Quiet_heap_never_logs()
    {
        Assert.False(GcCadenceProbe.ShouldLog(secondsSinceLastLog: 30f, gen0CollectionsInWindow: 0));
    }

    [Fact]
    public void Collections_log_once_per_window()
    {
        Assert.False(GcCadenceProbe.ShouldLog(secondsSinceLastLog: 4.9f, gen0CollectionsInWindow: 2));
        Assert.True(GcCadenceProbe.ShouldLog(secondsSinceLastLog: 5f, gen0CollectionsInWindow: 2));
    }
}
