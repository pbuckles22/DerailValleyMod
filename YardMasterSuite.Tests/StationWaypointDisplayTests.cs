using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class StationWaypointDisplayTests
{
    [Fact]
    public void Format_outside_zone_is_null()
    {
        Assert.Null(StationWaypointDisplay.Format(
            inZone: false,
            yardId: "SM",
            stationX: 100f,
            stationZ: 200f,
            playerX: 0f,
            playerZ: 0f));
    }

    [Fact]
    public void Format_in_zone_without_player_is_placeholder()
    {
        Assert.Equal(
            "— Station",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 100f,
                stationZ: 200f,
                playerX: null,
                playerZ: null));
    }

    [Fact]
    public void Format_in_zone_shows_bearing_and_distance_without_coords()
    {
        // Player east of station → walk west back to station.
        Assert.Equal(
            "Station SM W 100m",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 10f,
                stationZ: 20f,
                playerX: 110f,
                playerZ: 20f));
    }

    [Fact]
    public void Format_at_station_center_shows_here_without_coords()
    {
        Assert.Equal(
            "Station HB here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "HB",
                stationX: 50.2f,
                stationZ: 60.4f,
                playerX: 50.4f,
                playerZ: 60.1f));
    }

    [Fact]
    public void Format_missing_yard_uses_placeholder_id()
    {
        Assert.Equal(
            "Station — here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: null,
                stationX: 1f,
                stationZ: 2f,
                playerX: 1f,
                playerZ: 2f));
    }
}
