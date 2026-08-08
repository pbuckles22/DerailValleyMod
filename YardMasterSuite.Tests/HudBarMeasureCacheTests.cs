using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 2026-08-07 video: ~2.5 s rhythmic hitch in/out of cab and town — GC from OnGUI re-measure.
/// </summary>
public class HudBarMeasureCacheTests
{
    [Fact]
    public void Smoke_25s_cadence_stable_label_does_not_remeasure()
    {
        Assert.False(HudBarMeasureCache.NeedsRemeasure(
            cachedLabel: "Heading N  |  Station MF",
            label: "Heading N  |  Station MF",
            cachedScreenWidth: 1920,
            screenWidth: 1920));
    }

    [Fact]
    public void Label_change_or_resize_remeasures()
    {
        Assert.True(HudBarMeasureCache.NeedsRemeasure(null, "A", 1920, 1920));
        Assert.True(HudBarMeasureCache.NeedsRemeasure("A", "B", 1920, 1920));
        Assert.True(HudBarMeasureCache.NeedsRemeasure("A", "A", 1920, 1280));
    }

    [Fact]
    public void StripTags_skips_alloc_when_no_markup()
    {
        const string plain = "Speed 45";
        Assert.Same(plain, HudRichText.StripTags(plain));
    }

    [Fact]
    public void StripTags_removes_color_markup()
    {
        Assert.Equal("Load 97%", HudRichText.StripTags("<color=#ff0000>Load 97%</color>"));
    }
}
