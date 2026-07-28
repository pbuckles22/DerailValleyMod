using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MotorDebugOverrideTests
{
    public MotorDebugOverrideTests()
    {
        MotorDebugOverride.Clear();
    }

    [Fact]
    public void Cycle_off_heat50_critical_off()
    {
        const string id = "test-loco";
        Assert.Equal(MotorDebugOverride.Mode.Off, MotorDebugOverride.GetMode(id));

        MotorDebugOverride.Cycle(id);
        Assert.Equal(MotorDebugOverride.Mode.Heat50, MotorDebugOverride.GetMode(id));
        Assert.Equal(MotorStatus.Hot, MotorDebugOverride.ApplyStatus(id, MotorStatus.Ok));
        Assert.Equal(MotorCabTempBand.Warning, MotorDebugOverride.ApplyBand(id, MotorCabTempBand.Nominal));
        Assert.Equal(50f, MotorDebugOverride.ForcedHeatPercent(id));

        MotorDebugOverride.Cycle(id);
        Assert.Equal(MotorDebugOverride.Mode.Critical, MotorDebugOverride.GetMode(id));
        Assert.Equal(MotorCabTempBand.Critical, MotorDebugOverride.ApplyBand(id, null));
        Assert.Equal(100f, MotorDebugOverride.ForcedHeatPercent(id));

        MotorDebugOverride.Cycle(id);
        Assert.Equal(MotorDebugOverride.Mode.Off, MotorDebugOverride.GetMode(id));
        Assert.Equal(MotorStatus.Ok, MotorDebugOverride.ApplyStatus(id, MotorStatus.Ok));
        Assert.Null(MotorDebugOverride.ForcedHeatPercent(id));
    }
}
