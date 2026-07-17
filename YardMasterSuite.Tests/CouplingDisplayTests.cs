using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CouplingDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_either_end_unknown()
    {
        Assert.Equal("— cpl", CouplingDisplay.Format(null, null));
        Assert.Equal("— cpl", CouplingDisplay.Format(true, null));
        Assert.Equal("— cpl", CouplingDisplay.Format(null, false));
    }

    [Theory]
    [InlineData(true, true, "F+ R+")]
    [InlineData(true, false, "F+ R-")]
    [InlineData(false, true, "F- R+")]
    [InlineData(false, false, "F- R-")]
    public void Format_shows_front_and_rear(bool front, bool rear, string expected)
    {
        Assert.Equal(expected, CouplingDisplay.Format(front, rear));
    }
}
