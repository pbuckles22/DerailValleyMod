using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathPlanModeSelectTests
{
    [Fact]
    public void ForTrip_SameTownNamed_IsYard()
    {
        Assert.Equal(
            PathPlanMode.Yard,
            PathPlanModeSelect.ForTrip("SW-B4L", "SW-T11P"));
    }

    [Fact]
    public void ForTrip_AnonymousTtWithSessionYard_IsYard()
    {
        Assert.Equal(
            PathPlanMode.Yard,
            PathPlanModeSelect.ForTrip("SW-B4L", "#Y-#S1774#T", destYardOverride: "SW"));
    }

    [Fact]
    public void ForTrip_CrossCity_IsWorld()
    {
        Assert.Equal(
            PathPlanMode.World,
            PathPlanModeSelect.ForTrip("SW-B4L", "MF-A1P"));
    }

    [Fact]
    public void ForTrip_AnonymousWithoutOverride_IsWorld()
    {
        // No city on either side of the dest — stay World (fail closed for long-haul rules).
        Assert.Equal(
            PathPlanMode.World,
            PathPlanModeSelect.ForTrip("SW-B4L", "#Y-#S1774#T"));
    }
}
