using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class FluidDebugOverrideTests
{
    public FluidDebugOverrideTests()
    {
        FluidDebugOverride.Clear();
    }

    [Fact]
    public void Cycle_is_per_car_id()
    {
        FluidDebugOverride.Cycle("loco-a");
        Assert.Equal("fuel=100% oil=5%", FluidDebugOverride.StatusFragment("loco-a"));
        Assert.Equal("off", FluidDebugOverride.StatusFragment("loco-b"));
        Assert.Equal(72f, FluidDebugOverride.ApplyOil("loco-b", 72f));
        Assert.Equal(5f, FluidDebugOverride.ApplyOil("loco-a", 72f));
    }

    [Fact]
    public void Cycle_combined_presets_then_real()
    {
        FluidDebugOverride.Cycle("x");
        Assert.Equal(5f, FluidDebugOverride.ApplyOil("x", 72f));
        Assert.Equal(100f, FluidDebugOverride.ApplyFuel("x", 72f));

        FluidDebugOverride.Cycle("x");
        Assert.Equal(100f, FluidDebugOverride.ApplyOil("x", 72f));
        Assert.Equal(5f, FluidDebugOverride.ApplyFuel("x", 72f));

        FluidDebugOverride.Cycle("x");
        Assert.Equal(5f, FluidDebugOverride.ApplyFuel("x", 72f));
        Assert.Equal(5f, FluidDebugOverride.ApplyOil("x", 72f));

        FluidDebugOverride.Cycle("x");
        Assert.Equal(100f, FluidDebugOverride.ApplyFuel("x", 72f));
        Assert.Equal(100f, FluidDebugOverride.ApplyOil("x", 72f));

        FluidDebugOverride.Cycle("x");
        Assert.Equal("off", FluidDebugOverride.StatusFragment("x"));
        Assert.Equal(72f, FluidDebugOverride.ApplyOil("x", 72f));
    }

    [Fact]
    public void Clear_restores_passthrough()
    {
        FluidDebugOverride.Cycle("x");
        FluidDebugOverride.Clear();
        Assert.Equal(71f, FluidDebugOverride.ApplyOil("x", 71f));
        Assert.Equal("off", FluidDebugOverride.StatusFragment("x"));
    }
}

public class LoadDebugOverrideTests
{
    public LoadDebugOverrideTests()
    {
        LoadDebugOverride.Clear();
    }

    [Fact]
    public void Cycle_warn_then_critical_then_off_per_car()
    {
        LoadDebugOverride.Cycle("a");
        Assert.Equal(85f, LoadDebugOverride.Apply("a", 10f));
        Assert.Equal(10f, LoadDebugOverride.Apply("b", 10f));

        LoadDebugOverride.Cycle("a");
        Assert.Equal(97f, LoadDebugOverride.Apply("a", 10f));

        LoadDebugOverride.Cycle("a");
        Assert.Equal("off", LoadDebugOverride.StatusFragment("a"));
        Assert.Equal(10f, LoadDebugOverride.Apply("a", 10f));
    }
}

public class CouplerDebugOverrideTests
{
    public CouplerDebugOverrideTests()
    {
        CouplerDebugOverride.Clear();
    }

    [Fact]
    public void Cycle_front_rear_both_mu_then_off_per_car()
    {
        CouplerDebugOverride.Cycle("c1");
        Assert.Equal(CouplerLinkStatus.MuWarning, CouplerDebugOverride.ApplyFront("c1", CouplerLinkStatus.Open));
        Assert.Equal(CouplerLinkStatus.Linked, CouplerDebugOverride.ApplyRear("c1", CouplerLinkStatus.Open));
        Assert.Equal(CouplerLinkStatus.Open, CouplerDebugOverride.ApplyFront("c2", CouplerLinkStatus.Open));

        CouplerDebugOverride.Cycle("c1");
        Assert.Equal(CouplerLinkStatus.Linked, CouplerDebugOverride.ApplyFront("c1", null));
        Assert.Equal(CouplerLinkStatus.MuWarning, CouplerDebugOverride.ApplyRear("c1", null));

        CouplerDebugOverride.Cycle("c1");
        Assert.Equal(CouplerLinkStatus.MuWarning, CouplerDebugOverride.ApplyFront("c1", null));
        Assert.Equal(CouplerLinkStatus.MuWarning, CouplerDebugOverride.ApplyRear("c1", null));

        CouplerDebugOverride.Cycle("c1");
        Assert.Equal("off", CouplerDebugOverride.StatusFragment("c1"));
    }
}
