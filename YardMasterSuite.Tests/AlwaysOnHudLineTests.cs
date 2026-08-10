using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class AlwaysOnHudLineTests
{
    [Fact]
    public void Format_joins_version_heading_and_optional_chips_without_Pos()
    {
        var line = AlwaysOnHudLine.Format(
            "Heading NE",
            "Marked NNE 40m",
            "Station SM NE 84m",
            "Path OK",
            "Set Forward",
            "Clock 14:30",
            version: "v0.6.41");

        Assert.Equal(
            "v0.6.41  |  Heading NE  |  Marked NNE 40m  |  Station SM NE 84m  |  Path OK  |  Set Forward  |  Clock 14:30",
            line);
        Assert.DoesNotContain("Pos", line);
    }

    [Fact]
    public void Format_omits_blank_optional_chips()
    {
        var line = AlwaysOnHudLine.Format("Heading N", null, null, null, null, null);
        Assert.Equal("Heading N", line);
    }

    [Fact]
    public void Format_omits_blank_version()
    {
        var line = AlwaysOnHudLine.Format("Heading N", version: null);
        Assert.Equal("Heading N", line);
        Assert.DoesNotContain("v0.", line);
    }
}
