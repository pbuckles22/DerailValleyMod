using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class DebugHotkeyHudLineTests
{
    [Fact]
    public void Format_lists_cycle_keys()
    {
        var line = DebugHotkeyHudLine.Format();
        Assert.Contains("Shift+F1", line);
        Assert.Contains("F5 Lighter", line);
        Assert.Contains("F6 Load", line);
        Assert.Contains("F7 Cargo", line);
        Assert.Contains("F8 Fluids", line);
        Assert.Contains("F9 Couplers", line);
        Assert.Contains("F10 Motors", line);
        Assert.Contains("F11 Licenses", line);
        Assert.DoesNotContain("F12", line);
        Assert.DoesNotContain("PgUp", line);
    }
}

public class DebugHotkeyGateTests
{
    public DebugHotkeyGateTests()
    {
        DebugHotkeyGate.SetEnabled(true);
    }

    [Fact]
    public void Toggle_flips_enabled()
    {
        Assert.True(DebugHotkeyGate.Enabled);
        Assert.False(DebugHotkeyGate.Toggle());
        Assert.False(DebugHotkeyGate.Enabled);
        Assert.True(DebugHotkeyGate.Toggle());
    }
}
