using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LocalCarHudLineTests
{
    [Fact]
    public void Format_joins_local_car_segments()
    {
        var line = LocalCarHudLine.Format(
            "Pipe 2.0 bar",
            "Handbrake 1",
            "Couplers F+ R-",
            "Car 3",
            "Job FH-12");
        Assert.Equal(
            "Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12",
            line);
    }
}
