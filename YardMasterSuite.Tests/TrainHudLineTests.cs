using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TrainHudLineTests
{
    [Fact]
    public void Format_joins_center_weighted_ia_order()
    {
        var line = TrainHudLine.Format(
            "Fuel 67 %",
            "Oil 55 %",
            "Mass 240 t",
            "Grade +1.2 %",
            "Load 42 %",
            "Speed 36 km/h",
            "Limit 60",
            "Motors OK",
            "Handbrakes 3",
            "Cars 8",
            throttle: "Throttle 20 %",
            indy: "Indy 0 %",
            trainBrake: "TrainBrake 40 %",
            stress: "Stress 40 %");
        Assert.Equal(
            "Fuel 67 %  |  Oil 55 %  |  Mass 240 t  |  Grade +1.2 %  |  Load 42 %  |  Throttle 20 %  |  Indy 0 %  |  TrainBrake 40 %  |  Speed 36 km/h  |  Limit 60  |  Motors OK  |  Stress 40 %  |  Handbrakes 3  |  Cars 8",
            line);
    }

    [Fact]
    public void NullLine_is_all_placeholders_in_ia_order()
    {
        Assert.Equal(
            "— Fuel  |  — Oil  |  — Mass  |  — Grade  |  — Load  |  — Throttle  |  — Indy  |  — TrainBrake  |  — Speed  |  — Limit  |  — Motors  |  — Stress  |  — Handbrakes  |  — Cars",
            TrainHudLine.NullLine());
    }
}
