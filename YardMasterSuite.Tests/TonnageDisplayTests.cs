using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TonnageDisplayTests
{
    [Fact]
    public void KilogramsToTonnes_divides_by_1000()
    {
        Assert.Equal(77.45f, TonnageDisplay.KilogramsToTonnes(77450f), precision: 2);
    }

    [Fact]
    public void FormatFromKilograms_rounds_whole_tonnes()
    {
        Assert.Equal("— Mass", TonnageDisplay.FormatFromKilograms(null));
        Assert.Equal("Mass 77 t", TonnageDisplay.FormatFromKilograms(77450f));
        Assert.Equal("Mass 30 t", TonnageDisplay.FormatFromKilograms(30400f));
    }

    [Fact]
    public void FormatCarAndConsist_omits_consist_when_solo()
    {
        Assert.Equal(
            "Car 17 t",
            TonnageDisplay.FormatCarAndConsistFromKilograms(17000f, 17000f));
    }

    [Fact]
    public void FormatCarAndConsist_appends_trainset_total()
    {
        Assert.Equal(
            "Car 17 t  |  all cars 153 t",
            TonnageDisplay.FormatCarAndConsistFromKilograms(17000f, 153000f));
    }
}
