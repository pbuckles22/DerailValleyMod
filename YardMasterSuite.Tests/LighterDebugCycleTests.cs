using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class LighterDebugCycleTests
{
    [Theory]
    [InlineData(LighterDebugPhase.Real, LighterDebugPhase.InInventory)]
    [InlineData(LighterDebugPhase.InInventory, LighterDebugPhase.Removed)]
    [InlineData(LighterDebugPhase.Removed, LighterDebugPhase.Real)]
    public void Next_cycles_give_remove_real(LighterDebugPhase current, LighterDebugPhase expected)
    {
        Assert.Equal(expected, LighterDebugCycle.Next(current));
    }
}
