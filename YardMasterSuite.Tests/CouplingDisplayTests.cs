using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CouplingDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_either_end_unknown()
    {
        Assert.Equal("— Couplers", CouplingDisplay.Format(null, null));
        Assert.Equal("— Couplers", CouplingDisplay.Format(true, null));
        Assert.Equal("— Couplers", CouplingDisplay.Format(null, false));
    }

    [Theory]
    [InlineData(true, true, "Couplers F+ R+")]
    [InlineData(true, false, "Couplers F+ R-")]
    [InlineData(false, true, "Couplers F- R+")]
    [InlineData(false, false, "Couplers F- R-")]
    public void Format_shows_front_and_rear(bool front, bool rear, string expected)
    {
        Assert.Equal(expected, CouplingDisplay.Format(front, rear));
    }
}
