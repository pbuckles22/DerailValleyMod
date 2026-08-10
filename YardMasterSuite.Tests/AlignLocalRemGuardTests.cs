using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AlignLocalRemGuardTests
{
    [Fact]
    public void Smoke_Rejects_TenKmSameYardAlign()
    {
        Assert.True(
            AlignLocalRemGuard.IsImplausibleSameYardTrip(
                "SW",
                "SW",
                remainingMeters: 10800f));
    }

    [Fact]
    public void Allows_LocalYardRem()
    {
        Assert.False(
            AlignLocalRemGuard.IsImplausibleSameYardTrip(
                "SW",
                "SW",
                remainingMeters: 340f));
    }

    [Fact]
    public void Allows_CrossYardLongTrip()
    {
        Assert.False(
            AlignLocalRemGuard.IsImplausibleSameYardTrip(
                "MFMB",
                "SW",
                remainingMeters: 10800f));
    }

    [Fact]
    public void FailsOpen_WhenYardsUnknown()
    {
        Assert.False(
            AlignLocalRemGuard.IsImplausibleSameYardTrip(
                null,
                "SW",
                remainingMeters: 99999f));
    }
}
