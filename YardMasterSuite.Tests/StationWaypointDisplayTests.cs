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
            playerZ: 0f,
            atOffice: false));
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
                playerZ: null,
                atOffice: false));
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
                playerZ: 20f,
                atOffice: false));
    }

    [Fact]
    public void Format_at_office_shows_here_even_when_meters_from_anchor()
    {
        // Bundle C: "here" shares A.4 office footprint — not a 1 m point.
        Assert.Equal(
            "Station HB here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "HB",
                stationX: 50f,
                stationZ: 60f,
                playerX: 58f,
                playerZ: 60f,
                atOffice: true));
    }

    [Fact]
    public void Format_near_anchor_but_not_at_office_still_shows_bearing()
    {
        Assert.Equal(
            "Station SM E 1m",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 10f,
                stationZ: 20f,
                playerX: 9f,
                playerZ: 20f,
                atOffice: false));
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
                playerZ: 2f,
                atOffice: true));
    }

    [Fact]
    public void TryGetWalkPoint_at_office_is_here()
    {
        Assert.Equal("here", StationWaypointDisplay.TryGetWalkPoint(0f, 0f, 10f, 0f, atOffice: true));
    }

    [Fact]
    public void TryGetWalkPoint_not_at_office_is_bearing()
    {
        Assert.Equal("W", StationWaypointDisplay.TryGetWalkPoint(0f, 0f, 10f, 0f, atOffice: false));
    }
}
