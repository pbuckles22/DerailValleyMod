using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LocoTypeDisplayTests
{
    [Fact]
    public void Format_null_or_blank_omits_segment()
    {
        Assert.Null(LocoTypeDisplay.Format(null));
        Assert.Null(LocoTypeDisplay.Format(""));
        Assert.Null(LocoTypeDisplay.Format("   "));
    }

    [Fact]
    public void Format_strips_Loco_prefix_from_game_id()
    {
        Assert.Equal("Loco DE6", LocoTypeDisplay.Format("LocoDE6"));
        Assert.Equal("Loco DE2", LocoTypeDisplay.Format("LocoDE2"));
    }

    [Fact]
    public void Format_keeps_short_ids()
    {
        Assert.Equal("Loco DE6", LocoTypeDisplay.Format("DE6"));
        Assert.Equal("Loco S060", LocoTypeDisplay.Format("S060"));
    }
}
