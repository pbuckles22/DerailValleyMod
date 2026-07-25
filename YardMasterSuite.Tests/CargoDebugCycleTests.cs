using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class CargoDebugCycleTests
{
    [Theory]
    [InlineData(true, CargoDebugAction.Unload)]
    [InlineData(false, CargoDebugAction.Load)]
    public void NextAction_toggles_unload_and_load(bool hasCargo, CargoDebugAction expected)
    {
        Assert.Equal(expected, CargoDebugCycle.NextAction(hasCargo));
    }
}
