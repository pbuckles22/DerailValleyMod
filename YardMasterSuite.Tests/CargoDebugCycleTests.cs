using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class CargoDebugCycleTests
{
    [Theory]
    [InlineData(true, CargoDebugAction.Unload)]
    [InlineData(false, CargoDebugAction.Load)]
    public void NextAction_any_loaded_unloads_else_loads(bool anyFreightHasCargo, CargoDebugAction expected)
    {
        Assert.Equal(expected, CargoDebugCycle.NextAction(anyFreightHasCargo));
    }
}
