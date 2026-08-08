using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.4.20.1 OnGUI A/B: hide visuals without disabling the mod (Update still runs).
/// </summary>
public class HudDrawGateTests
{
    public HudDrawGateTests()
    {
        HudDrawGate.ResetForTests();
    }

    [Fact]
    public void Defaults_to_drawing()
    {
        Assert.True(HudDrawGate.DrawVisuals);
    }

    [Fact]
    public void Toggle_flips_draw_visuals()
    {
        HudDrawGate.Toggle();
        Assert.False(HudDrawGate.DrawVisuals);
        HudDrawGate.Toggle();
        Assert.True(HudDrawGate.DrawVisuals);
    }
}
