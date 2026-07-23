using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class NextStationDisplayTests
{
    [Fact]
    public void Format_null_when_fluids_ok()
    {
        Assert.Null(NextStationDisplay.Format(
            fluidsLow: false,
            stationLabel: "Food Factory",
            distanceMeters: 12_500f));
    }

    [Fact]
    public void Format_null_when_station_or_distance_unknown()
    {
        Assert.Null(NextStationDisplay.Format(
            fluidsLow: true,
            stationLabel: null,
            distanceMeters: 1000f));
        Assert.Null(NextStationDisplay.Format(
            fluidsLow: true,
            stationLabel: "SM",
            distanceMeters: null));
        Assert.Null(NextStationDisplay.Format(
            fluidsLow: true,
            stationLabel: "  ",
            distanceMeters: 1000f));
    }

    [Fact]
    public void Format_shows_next_with_km()
    {
        Assert.Equal(
            "Next: Food Factory [12.5 km]",
            NextStationDisplay.Format(
                fluidsLow: true,
                stationLabel: "Food Factory",
                distanceMeters: 12_500f));
        Assert.Equal(
            "Next: SM [0.8 km]",
            NextStationDisplay.Format(
                fluidsLow: true,
                stationLabel: "SM",
                distanceMeters: 750f));
    }
}
