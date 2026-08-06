using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke FAIL @ 0.6.31: 560px panel still too small on high-res. Target 1120 with screen clamp.
/// </summary>
public class YardMiniMapPanelLayoutTests
{
    [Fact]
    public void Smoke_031_default_is_1120()
    {
        Assert.Equal(1120f, YardMiniMapPanelLayout.DefaultPanelSizePixels);
    }

    [Fact]
    public void Smoke_031_4k_gets_full_1120()
    {
        var size = YardMiniMapPanelLayout.ResolveSquarePanelSize(3840f, 2160f, marginPixels: 16f);
        Assert.Equal(1120f, size);
    }

    [Fact]
    public void Smoke_031_1080p_clamps_to_screen()
    {
        // 1080 - 32 = 1048
        var size = YardMiniMapPanelLayout.ResolveSquarePanelSize(1920f, 1080f, marginPixels: 16f);
        Assert.Equal(1048f, size);
        Assert.True(size < YardMiniMapPanelLayout.DefaultPanelSizePixels);
    }

    [Fact]
    public void Twice_560_is_default_target()
    {
        Assert.Equal(560f * 2f, YardMiniMapPanelLayout.DefaultPanelSizePixels);
    }
}
