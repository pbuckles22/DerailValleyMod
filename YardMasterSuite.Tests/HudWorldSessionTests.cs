using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class HudWorldSessionTests
{
    [Fact]
    public void IsActive_requires_player_in_world()
    {
        Assert.False(HudWorldSession.IsActive(playerTransformPresent: false));
        Assert.True(HudWorldSession.IsActive(playerTransformPresent: true));
    }
}
