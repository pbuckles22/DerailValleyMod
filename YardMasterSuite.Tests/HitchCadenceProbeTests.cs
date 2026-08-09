using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HitchCadenceProbeTests
{
    public HitchCadenceProbeTests()
    {
        HitchCadenceProbe.ResetForTests();
    }

    [Fact]
    public void FormatSummary_includes_counters_and_gc_keys()
    {
        HitchCadenceProbe.NotePick(3);
        HitchCadenceProbe.NoteFotKillSkip();
        HitchCadenceProbe.NoteLimitFromGeometry();

        var line = HitchCadenceProbe.FormatSummary(12.5f, fotEnabled: false, hudDraw: true, rosterCount: 3);

        Assert.StartsWith("T2 hitch-sum:", line);
        Assert.Contains("fot=0", line);
        Assert.Contains("draw=1", line);
        Assert.Contains("pick=1", line);
        Assert.Contains("fotKill=1", line);
        Assert.Contains("limGeom=1", line);
        Assert.Contains("roster=3", line);
        Assert.Contains("dGc0=", line);
        Assert.Contains("heapMB=", line);
    }

    [Fact]
    public void TickSummary_emits_only_after_interval()
    {
        Assert.Null(HitchCadenceProbe.TickSummary(1f, false, true, 0));
        Assert.Null(HitchCadenceProbe.TickSummary(3f, false, true, 0));
        var line = HitchCadenceProbe.TickSummary(1f + HitchCadenceProbe.SummaryIntervalSeconds + 0.1f, false, true, 0);
        Assert.NotNull(line);
        Assert.StartsWith("T2 hitch-sum:", line);
    }

    [Fact]
    public void NoteFrameDelta_emits_when_above_threshold()
    {
        Assert.Null(HitchCadenceProbe.NoteFrameDelta(0.01f, 1f));
        var spike = HitchCadenceProbe.NoteFrameDelta(0.08f, 1f);
        Assert.NotNull(spike);
        Assert.StartsWith("T2 hitch-spike:", spike);
        Assert.Contains("dt=0.080", spike);
        Assert.Equal(1, HitchCadenceProbe.SpikeCount);
    }

    [Fact]
    public void FormatFotRefreshLine_includes_timing()
    {
        var line = HitchCadenceProbe.FormatFotRefreshLine(true, rawSignCount: 40, rosterCount: 5, elapsedMs: 12.3);
        Assert.StartsWith("T2 hitch-fot:", line);
        Assert.Contains("raw=40", line);
        Assert.Contains("roster=5", line);
        Assert.Contains("ms=12.3", line);
    }
}
