using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SessionDistanceTests
{
    [Fact]
    public void Step_integrates_speed_over_time()
    {
        // 36 km/h = 10 m/s Ã— 10 s = 100 m
        var meters = SessionDistance.Step(0f, speedKmh: 36f, deltaSeconds: 10f);
        Assert.Equal(100f, meters, precision: 2);
    }

    [Fact]
    public void Step_ignores_stopped_or_non_positive_dt()
    {
        Assert.Equal(50f, SessionDistance.Step(50f, speedKmh: 0f, deltaSeconds: 5f));
        Assert.Equal(50f, SessionDistance.Step(50f, speedKmh: 40f, deltaSeconds: 0f));
        Assert.Equal(50f, SessionDistance.Step(50f, speedKmh: -10f, deltaSeconds: 5f));
    }

    [Fact]
    public void Format_uses_meters_under_one_km_then_km()
    {
        Assert.Equal("Drive 850 m", SessionDistance.Format(850f));
        Assert.Equal("Drive 1.2 km", SessionDistance.Format(1200f));
        Assert.Equal("Drive 0 m", SessionDistance.Format(0f));
    }

    [Fact]
    public void TrainHudLine_can_append_drive_chip()
    {
        var line = TrainHudLine.Format(
            "Fuel 67 %",
            "Oil 55 %",
            "Mass 38 t",
            "Grade 0.0 %",
            "Load 0 %",
            "Speed 40 km/h",
            "Limit 60",
            "Motors OK",
            "Handbrakes 0",
            "Cars 0",
            drive: "Drive 3.4 km");
        Assert.Contains("Cars 0  |  Drive 3.4 km", line);
    }
}
