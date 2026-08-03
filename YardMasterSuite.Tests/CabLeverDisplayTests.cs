using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class CabLeverDisplayTests
{
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 50f)]
    [InlineData(1f, 100f)]
    [InlineData(1.2f, 100f)]
    [InlineData(-0.1f, 0f)]
    public void PercentFromNormalized_clamps(float input, float expected)
    {
        Assert.Equal(expected, CabLeverDisplay.PercentFromNormalized(input));
    }

    [Fact]
    public void PercentFromNormalized_null_stays_null()
    {
        Assert.Null(CabLeverDisplay.PercentFromNormalized(null));
    }

    [Fact]
    public void Format_labels_and_placeholders()
    {
        Assert.Equal("Throttle 42 %", CabLeverDisplay.FormatThrottle(42f));
        Assert.Equal("Indy 10 %", CabLeverDisplay.FormatIndy(10f));
        Assert.Equal("TrainBrake 75 %", CabLeverDisplay.FormatTrainBrake(75f));
        Assert.Equal("— Throttle", CabLeverDisplay.FormatThrottle(null));
        Assert.Equal("— Indy", CabLeverDisplay.FormatIndy(null));
        Assert.Equal("— TrainBrake", CabLeverDisplay.FormatTrainBrake(null));
    }
}
