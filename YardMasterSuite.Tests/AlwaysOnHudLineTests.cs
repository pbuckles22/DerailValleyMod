using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AlwaysOnHudLineTests
{
    [Fact]
    public void Format_joins_version_heading_and_optional_chips_without_Pos()
    {
        var line = AlwaysOnHudLine.Format(
            "v0.4.30",
            "Heading NE",
            "Marked NNE 40m",
            "Station SM NE 84m");

        Assert.Equal(
            "v0.4.30  |  Heading NE  |  Marked NNE 40m  |  Station SM NE 84m",
            line);
        Assert.DoesNotContain("Pos", line);
    }

    [Fact]
    public void Format_omits_blank_optional_chips()
    {
        var line = AlwaysOnHudLine.Format("v0.4.30", "Heading N", null, null);
        Assert.Equal("v0.4.30  |  Heading N", line);
    }
}
