using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — office bounds FoT only on town enter (yard change).</summary>
public class OfficeBoundsCachePolicyTests
{
    [Fact]
    public void ShouldResolve_first_town_true()
    {
        Assert.True(OfficeBoundsCachePolicy.ShouldResolve(null, "FF", hasCache: false));
    }

    [Fact]
    public void ShouldResolve_same_town_with_cache_false()
    {
        Assert.False(OfficeBoundsCachePolicy.ShouldResolve("FF", "FF", hasCache: true));
    }

    [Fact]
    public void ShouldResolve_town_change_true()
    {
        Assert.True(OfficeBoundsCachePolicy.ShouldResolve("FF", "SM", hasCache: true));
    }

    [Fact]
    public void ShouldResolve_empty_yard_false()
    {
        Assert.False(OfficeBoundsCachePolicy.ShouldResolve("FF", null, hasCache: true));
        Assert.False(OfficeBoundsCachePolicy.ShouldResolve(null, "  ", hasCache: false));
    }

    [Fact]
    public void ShouldResolve_case_insensitive_same_town()
    {
        Assert.False(OfficeBoundsCachePolicy.ShouldResolve("ff", "FF", hasCache: true));
    }
}
