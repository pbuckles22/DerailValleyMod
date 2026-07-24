using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ParkMarkDisplayTests
{
    [Fact]
    public void FormatReturn_no_mark_is_null()
    {
        Assert.Null(ParkMarkDisplay.FormatReturn(null, null, 10f, 20f));
    }

    [Fact]
    public void FormatReturn_mark_without_player_is_placeholder()
    {
        Assert.Equal("— Marked", ParkMarkDisplay.FormatReturn(10f, 20f, null, null));
    }

    [Fact]
    public void FormatReturn_east_of_mark_shows_west_bearing()
    {
        // Player at (100, 0), mark at (0, 0) → return direction is −X = west.
        Assert.Equal("Marked W 100m", ParkMarkDisplay.FormatReturn(0f, 0f, 100f, 0f));
    }

    [Fact]
    public void FormatReturn_north_of_mark_shows_south_bearing()
    {
        // Player at (0, 50), mark at (0, 0) → return is −Z = south.
        Assert.Equal("Marked S 50m", ParkMarkDisplay.FormatReturn(0f, 0f, 0f, 50f));
    }

    [Fact]
    public void FormatReturn_at_mark_shows_here()
    {
        Assert.Equal("Marked here", ParkMarkDisplay.FormatReturn(10.2f, 20.4f, 10.4f, 20.1f));
    }

    [Fact]
    public void FormatCoords_rounds_xz()
    {
        Assert.Equal("Marked 10, 200", ParkMarkDisplay.FormatCoords(10.4f, 200.1f));
    }

    [Fact]
    public void TryGetReturnPoint_maps_bearing_and_here()
    {
        Assert.Equal("W", ParkMarkDisplay.TryGetReturnPoint(0f, 0f, 100f, 0f));
        Assert.Equal("here", ParkMarkDisplay.TryGetReturnPoint(10f, 20f, 10.2f, 20.1f));
    }
}

public class ParkMarkSessionTests
{
    public ParkMarkSessionTests()
    {
        ParkMarkSession.Clear();
    }

    [Fact]
    public void Set_then_TryGet_returns_mark()
    {
        ParkMarkSession.Set(12.5f, -40f);
        Assert.True(ParkMarkSession.HasMark);
        Assert.True(ParkMarkSession.TryGet(out var x, out var z));
        Assert.Equal(12.5f, x);
        Assert.Equal(-40f, z);
    }

    [Fact]
    public void Clear_removes_mark()
    {
        ParkMarkSession.Set(1f, 2f);
        ParkMarkSession.Clear();
        Assert.False(ParkMarkSession.HasMark);
        Assert.False(ParkMarkSession.TryGet(out _, out _));
    }

    [Fact]
    public void Set_with_y_preserves_stand_height()
    {
        ParkMarkSession.Set(12.5f, 3.2f, -40f);
        Assert.True(ParkMarkSession.TryGet(out var x, out var y, out var z));
        Assert.Equal(12.5f, x);
        Assert.Equal(3.2f, y);
        Assert.Equal(-40f, z);
    }

    [Fact]
    public void Set_replaces_previous_mark()
    {
        ParkMarkSession.Set(1f, 2f);
        ParkMarkSession.Set(9f, 8f);
        Assert.True(ParkMarkSession.TryGet(out var x, out var z));
        Assert.Equal(9f, x);
        Assert.Equal(8f, z);
    }
}
