using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HudPerfLogTests
{
    [Fact]
    public void Format_train_and_limit_lines_include_phase_ms()
    {
        Assert.Equal(
            "T2 perf train: total=12ms fluids=1ms limit=10ms massLevers=0ms rest=1ms",
            HudPerfLog.FormatTrainBar(12, 1, 10, 0, 1));
        Assert.Equal(
            "T2 perf limit: total=1800ms fot=1500ms path=20ms walk=280ms signs=40 segs=9",
            HudPerfLog.FormatLimitScan(1800, 1500, 20, 280, 40, 9));
    }
}
