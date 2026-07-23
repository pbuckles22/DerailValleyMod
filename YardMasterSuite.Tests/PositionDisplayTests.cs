using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PositionDisplayTests
{
    [Fact]
    public void Format_null_is_placeholder()
    {
        Assert.Equal("— Pos", PositionDisplay.Format(null, null));
        Assert.Equal("— Pos", PositionDisplay.Format(1f, null));
    }

    [Fact]
    public void Format_rounds_xz_map_only()
    {
        Assert.Equal("Pos 10, 200", PositionDisplay.Format(10.4f, 200.1f));
    }
}
