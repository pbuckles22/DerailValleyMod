using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LookAtTargetingTests
{
    [Fact]
    public void MaxDistance_is_250m_for_yard_scouting()
    {
        Assert.Equal(250f, LookAtTargeting.MaxDistanceMeters);
    }

    [Fact]
    public void SphereRadius_is_point_one_five_meters()
    {
        Assert.Equal(0.15f, LookAtTargeting.SphereRadiusMeters);
    }
}
