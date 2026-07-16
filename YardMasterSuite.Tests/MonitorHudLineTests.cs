using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MonitorHudLineTests
{
    [Fact]
    public void Join_extends_left_to_right_with_separator()
    {
        var line = MonitorHudLine.Join(new[] { "15 km/h", "+1.2 %", "240 t" });
        Assert.Equal("15 km/h  |  +1.2 %  |  240 t", line);
    }

    [Fact]
    public void Join_skips_blank_segments()
    {
        Assert.Equal("15 km/h", MonitorHudLine.Join(new[] { "15 km/h", "  ", null! }));
    }
}
