using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CargoDisplayTests
{
    [Fact]
    public void Format_omits_segment_for_locos()
    {
        Assert.Null(CargoDisplay.Format(isLoco: true, "SteelRails"));
    }

    [Fact]
    public void Format_empty_or_none_is_unknown()
    {
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, null));
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, "None"));
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, "EmptyGoorsk"));
    }

    [Fact]
    public void Format_humanizes_enum_names()
    {
        Assert.Equal("Cargo Steel Rails", CargoDisplay.Format(isLoco: false, "SteelRails"));
        Assert.Equal("Cargo Iron Ore", CargoDisplay.Format(isLoco: false, "IronOre"));
        Assert.Equal("Cargo Steel Rolls", CargoDisplay.Format(isLoco: false, "SteelRolls"));
    }
}
