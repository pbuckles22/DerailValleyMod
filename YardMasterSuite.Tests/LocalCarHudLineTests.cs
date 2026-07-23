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
            "Job FH-12",
            "Track SM-O6I");
        Assert.Equal(
            "Pipe 2.0 bar  |  Handbrake 1  |  Couplers F+ R-  |  Car 3  |  Job FH-12  |  Track SM-O6I",
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
            "Track C-06S",
            cargo: "Cargo Steel Rails");
        Assert.Equal(
            "Pipe 1.0 bar  |  Handbrake 0  |  Couplers F+ R+  |  Car XX  |  Job SM-SU-46  |  Track C-06S  |  Cargo Steel Rails",
            freight);

        var loco = LocalCarHudLine.Format(
            "Pipe 5.0 bar",
            "Handbrake 0",
            "Couplers F- R-",
            "Car N/A",
            "— Job",
            track: null,
            cargo: null,
            locoType: "Loco DE6");
        Assert.Equal(
            "Pipe 5.0 bar  |  Handbrake 0  |  Couplers F- R-  |  Car N/A  |  — Job  |  Loco DE6",
            loco);
    }

    [Fact]
    public void Format_omits_blank_track_segment()
    {
        var line = LocalCarHudLine.Format(
            "Pipe 5.0 bar",
            "Handbrake 0",
            "Couplers F- R-",
            "Car N/A",
            "— Job",
            track: null,
            locoType: "Loco DE2");
        Assert.Equal(
            "Pipe 5.0 bar  |  Handbrake 0  |  Couplers F- R-  |  Car N/A  |  — Job  |  Loco DE2",
            line);
        Assert.DoesNotContain("Track", line);
    }
}
