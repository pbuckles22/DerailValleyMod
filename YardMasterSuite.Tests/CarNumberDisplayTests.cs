using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CarNumberDisplayTests
{
    [Fact]
    public void Format_loco_is_na()
    {
        Assert.Equal("Car N/A", CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: null));
        Assert.Equal("Car N/A", CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: 1));
    }

    [Fact]
    public void Format_shows_xx_when_not_on_usable_train()
    {
        Assert.Equal("Car XX", CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: null));
    }

    [Theory]
    [InlineData(1, "Car 1")]
    [InlineData(7, "Car 7")]
    public void Format_shows_freight_number(int number, string expected)
    {
        Assert.Equal(expected, CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: number));
    }

    [Fact]
    public void FreightNumberFromLoco_null_for_loco_itself()
    {
        // Loco + 5 freight
        var flags = new[] { true, false, false, false, false, false };
        Assert.Null(CarNumberDisplay.FreightNumberFromLoco(0, 0, flags));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, 2, 2)]
    [InlineData(0, 5, 5)]
    public void FreightNumberFromLoco_excludes_loco_from_count(int locoIndex, int carIndex, int expected)
    {
        var flags = new[] { true, false, false, false, false, false };
        Assert.Equal(expected, CarNumberDisplay.FreightNumberFromLoco(locoIndex, carIndex, flags));
    }

    [Fact]
    public void FreightNumberFromLoco_works_when_loco_not_at_end()
    {
        // freight, freight, loco, freight
        var flags = new[] { false, false, true, false };
        Assert.Equal(2, CarNumberDisplay.FreightNumberFromLoco(2, 0, flags));
        Assert.Equal(1, CarNumberDisplay.FreightNumberFromLoco(2, 1, flags));
        Assert.Equal(1, CarNumberDisplay.FreightNumberFromLoco(2, 3, flags));
    }
}
