using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class SwitchListClearPromptTests
{
    [Fact]
    public void FormatAtSwitch_is_switch_not_safe_zone_pin()
    {
        Assert.Equal("At switch · keep going · 10m", SwitchListClearPrompt.FormatAtSwitch(10));
        Assert.Equal("At switch · keep going", SwitchListClearPrompt.FormatAtSwitch(0));
        Assert.Equal(SwitchListClearPrompt.ClearedCaption, "CLEARED · Next");
        Assert.DoesNotContain("Safe zone", SwitchListClearPrompt.FormatAtSwitch(10));
    }

    [Fact]
    public void Smoke_MidSwitch_EightMeters_With_R9_Coaches_OneMore()
    {
        Assert.True(SwitchListClearPrompt.IsInsideDangerCircle(8f, 9f));
        Assert.Equal(
            "At switch · keep going · 1m",
            SwitchListClearPrompt.FormatAtSwitch(
                RadialSwitchClearance.MetersToClearRimAlongAxis(8f, 9f)));
    }
}
