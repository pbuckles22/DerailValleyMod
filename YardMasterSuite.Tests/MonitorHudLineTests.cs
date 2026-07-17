using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MonitorHudLineTests
{
    [Fact]
    public void Join_extends_left_to_right_with_separator()
    {
        var line = MonitorHudLine.Join(new[] { "Speed 15 km/h", "Grade +1.2 %", "Mass 240 t" });
        Assert.Equal("Speed 15 km/h  |  Grade +1.2 %  |  Mass 240 t", line);
    }

    [Fact]
    public void Join_skips_blank_segments()
    {
        Assert.Equal("Speed 15 km/h", MonitorHudLine.Join(new[] { "Speed 15 km/h", "  ", null! }));
    }
}
