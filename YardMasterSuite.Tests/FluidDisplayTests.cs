using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class FluidDisplayTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0f, 0f)]
    [InlineData(100f, 0f)]
    [InlineData(null, 1000f)]
    public void PercentFromAmount_unknown_when_inputs_invalid(float? amount, float? capacity)
    {
        Assert.Null(FluidDisplay.PercentFromAmount(amount, capacity));
    }

    [Fact]
    public void PercentFromAmount_is_amount_over_capacity()
    {
        Assert.Equal(42f, FluidDisplay.PercentFromAmount(420f, 1000f));
        Assert.Equal(100f, FluidDisplay.PercentFromAmount(1000f, 1000f));
        Assert.Equal(0f, FluidDisplay.PercentFromAmount(0f, 1000f));
    }

    [Fact]
    public void PercentFromAmount_clamps_above_100()
    {
        Assert.Equal(100f, FluidDisplay.PercentFromAmount(1200f, 1000f));
    }

    [Fact]
    public void PercentFromNormalized_scales_0_1_to_percent()
    {
        Assert.Null(FluidDisplay.PercentFromNormalized(null));
        Assert.Equal(0f, FluidDisplay.PercentFromNormalized(0f));
        Assert.Equal(42f, FluidDisplay.PercentFromNormalized(0.42f));
        Assert.Equal(100f, FluidDisplay.PercentFromNormalized(1.2f));
    }

    [Fact]
    public void Format_shows_placeholder_and_whole_percent()
    {
        Assert.Equal("— Fuel", FluidDisplay.FormatFuel(null));
        Assert.Equal("— Oil", FluidDisplay.FormatOil(null));
        Assert.Equal("Fuel 0 %", FluidDisplay.FormatFuel(0f));
        Assert.Equal("Oil 55 %", FluidDisplay.FormatOil(55.4f));
        Assert.Equal("Fuel 67 %", FluidDisplay.FormatFuel(66.6f));
    }

    [Fact]
    public void Format_plain_has_no_color_tags()
    {
        Assert.Equal("Fuel 15 %", FluidDisplay.FormatFuel(15f));
        Assert.Equal("Oil 10 %", FluidDisplay.FormatOil(10f));
    }

    [Fact]
    public void FormatHud_yellow_when_either_fluid_below_20()
    {
        Assert.Equal("Fuel 20 %", FluidDisplay.FormatFuelHud(20f, 50f));
        Assert.Equal("Oil 50 %", FluidDisplay.FormatOilHud(20f, 50f));

        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Fuel 19 %</color>",
            FluidDisplay.FormatFuelHud(19f, 50f));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Oil 50 %</color>",
            FluidDisplay.FormatOilHud(19f, 50f));

        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Fuel 67 %</color>",
            FluidDisplay.FormatFuelHud(67f, 10f));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Oil 10 %</color>",
            FluidDisplay.FormatOilHud(67f, 10f));
    }

    [Fact]
    public void FormatHud_red_when_either_fluid_below_5()
    {
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Fuel 5 %</color>",
            FluidDisplay.FormatFuelHud(5f, 50f));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Oil 50 %</color>",
            FluidDisplay.FormatOilHud(5f, 50f));

        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Fuel 4 %</color>",
            FluidDisplay.FormatFuelHud(4f, 50f));
        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Oil 50 %</color>",
            FluidDisplay.FormatOilHud(4f, 50f));

        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Fuel 67 %</color>",
            FluidDisplay.FormatFuelHud(67f, 3f));
        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Oil 3 %</color>",
            FluidDisplay.FormatOilHud(67f, 3f));
    }

    [Fact]
    public void FormatHud_placeholder_stays_plain_even_when_peer_low()
    {
        Assert.Equal("— Oil", FluidDisplay.FormatOilHud(10f, null));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Fuel 10 %</color>",
            FluidDisplay.FormatFuelHud(10f, null));
    }

    [Fact]
    public void IsLow_and_IsCritical_use_whole_percent_bands()
    {
        Assert.False(FluidDisplay.IsLow(null));
        Assert.False(FluidDisplay.IsCritical(null));
        Assert.False(FluidDisplay.IsLow(20f));
        Assert.True(FluidDisplay.IsLow(19.4f));
        Assert.False(FluidDisplay.IsCritical(5f));
        Assert.True(FluidDisplay.IsCritical(4.4f));
        Assert.True(FluidDisplay.IsLow(4f));
    }
}
