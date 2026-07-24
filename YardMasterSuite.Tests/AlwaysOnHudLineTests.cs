using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AlwaysOnHudLineTests
{
    [Fact]
    public void Format_joins_heading_and_optional_chips_without_version_or_Pos()
    {
        var line = AlwaysOnHudLine.Format(
            "Heading NE",
            "Marked NNE 40m",
            "Station SM NE 84m");

        Assert.Equal(
            "Heading NE  |  Marked NNE 40m  |  Station SM NE 84m",
            line);
        Assert.DoesNotContain("Pos", line);
        Assert.DoesNotContain("v0.", line);
    }

    [Fact]
    public void Format_omits_blank_optional_chips()
    {
        var line = AlwaysOnHudLine.Format("Heading N", null, null);
        Assert.Equal("Heading N", line);
    }
}
