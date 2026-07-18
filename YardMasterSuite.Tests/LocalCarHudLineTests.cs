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

    [Fact]
    public void Format_appends_cargo_and_loco_when_present()
    {
        var freight = LocalCarHudLine.Format(
            "Pipe 1.0 bar",
            "Handbrake 0",
            "Couplers F+ R+",
            "Car XX",
            "Job SM-SU-46",
            cargo: "Cargo Steel Rails");
        Assert.Equal(
            "Pipe 1.0 bar  |  Handbrake 0  |  Couplers F+ R+  |  Car XX  |  Job SM-SU-46  |  Cargo Steel Rails",
            freight);

        var loco = LocalCarHudLine.Format(
            "Pipe 5.0 bar",
            "Handbrake 0",
            "Couplers F- R-",
            "Car N/A",
            "— Job",
            cargo: null,
            locoType: "Loco DE6");
        Assert.Equal(
            "Pipe 5.0 bar  |  Handbrake 0  |  Couplers F- R-  |  Car N/A  |  — Job  |  Loco DE6",
            loco);
    }
}
