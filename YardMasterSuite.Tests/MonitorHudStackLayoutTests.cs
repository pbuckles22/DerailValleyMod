using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MonitorHudStackLayoutTests
{
    [Fact]
    public void StackBottom_always_on_only()
    {
        // Pad 12 + bar 28 = 40
        Assert.Equal(40f, MonitorHudStackLayout.StackBottomGuiY(false, false, false));
    }

    [Fact]
    public void StackBottom_all_bars()
    {
        // 12 + 3*(28+4) + 28 = 12 + 96 + 28 = 136
        Assert.Equal(136f, MonitorHudStackLayout.StackBottomGuiY(true, true, true));
    }

    [Fact]
    public void StickyRowTop_adds_gap()
    {
        Assert.Equal(48f, MonitorHudStackLayout.StickyRowTopGuiY(40f));
        Assert.Equal(50f, MonitorHudStackLayout.StickyRowTopGuiY(40f, 10f));
    }
}

public class ArStickyRowPlacementTests
{
    [Fact]
    public void PinScreenYToStickyRow_converts_gui_center_to_unity_screen_y()
    {
        float screenY = 999f;
        // GUI center 100 from top on 600h → Unity Y = 500
        ArStickyRowPlacement.PinScreenYToStickyRow(100f, 600f, ref screenY);
        Assert.Equal(500f, screenY);
    }

    [Fact]
    public void Pin_does_not_move_x_responsibility_caller_keeps_edge_x()
    {
        // Documented contract: only Y is pinned; X stays from A.1 projection.
        float x = 28f;
        float y = 300f;
        ArStickyRowPlacement.PinScreenYToStickyRow(48f + 24f, 600f, ref y);
        Assert.Equal(28f, x);
        Assert.Equal(600f - 72f, y);
    }

    [Fact]
    public void MarkerTopGuiY_aligns_to_sticky_strip()
    {
        Assert.Equal(48f, ArStickyRowPlacement.MarkerTopGuiY(48f, 60f));
    }
}
