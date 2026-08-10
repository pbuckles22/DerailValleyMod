using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteExitTravelMatchTests
{
    [Fact]
    public void Format_match_and_opposite()
    {
        Assert.Equal("match NNW", RouteExitTravelMatch.Format("Exit NNW", "NNW", 12f));
        Assert.Contains("opposite", RouteExitTravelMatch.Format("Exit NNW", "SSE", 12f));
        Assert.Equal("idle→NNW", RouteExitTravelMatch.Format("Exit NNW", "NNW", 0f));
    }

    [Fact]
    public void NormalizePoint_strips_exit_prefix()
    {
        Assert.Equal("NNW", RouteExitTravelMatch.NormalizePoint("Exit NNW"));
        Assert.Equal("NE", RouteExitTravelMatch.NormalizePoint("ne"));
    }
}
