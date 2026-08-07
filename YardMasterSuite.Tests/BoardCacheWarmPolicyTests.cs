using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BoardCacheWarmPolicyTests
{
    [Fact]
    public void Align_sync_budget_is_half_second_for_up_to_eight_boards()
    {
        Assert.Equal(8, BoardCacheWarmPolicy.MaxOnRouteSigns);
        Assert.Equal(500, BoardCacheWarmPolicy.AlignBudgetMilliseconds);
        Assert.True(BoardCacheWarmPolicy.ContinueAlignAttach(0, 0));
        Assert.True(BoardCacheWarmPolicy.ContinueAlignAttach(7, 499));
        Assert.False(BoardCacheWarmPolicy.ContinueAlignAttach(8, 10));
        Assert.False(BoardCacheWarmPolicy.ContinueAlignAttach(2, 500));
    }

    [Fact]
    public void Align_warm_complete_at_four_on_route()
    {
        Assert.False(BoardCacheWarmPolicy.AlignWarmComplete(3));
        Assert.True(BoardCacheWarmPolicy.AlignWarmComplete(4));
    }
}
