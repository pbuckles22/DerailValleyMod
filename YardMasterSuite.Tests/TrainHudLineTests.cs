using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class TrainHudLineTests
{
    [Fact]
    public void Format_joins_train_total_segments()
    {
        var line = TrainHudLine.Format(
            "Speed 36 km/h",
            "Limit 60",
            "Grade +1.2 %",
            "Mass 240 t",
            "Cars 8",
            "Handbrakes 3",
            "Load 42 %",
            "Motors OK",
            "Fuel 67 %",
            "Oil 55 %");
        Assert.Equal(
            "Speed 36 km/h  |  Limit 60  |  Grade +1.2 %  |  Mass 240 t  |  Cars 8  |  Handbrakes 3  |  Load 42 %  |  Motors OK  |  Fuel 67 %  |  Oil 55 %",
            line);
    }

    [Fact]
    public void NullLine_is_all_placeholders()
    {
        Assert.Equal(
            "— Speed  |  — Limit  |  — Grade  |  — Mass  |  — Cars  |  — Handbrakes  |  — Load  |  — Motors  |  — Fuel  |  — Oil",
            TrainHudLine.NullLine());
    }
}
