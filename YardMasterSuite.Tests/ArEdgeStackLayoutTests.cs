using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ArEdgeStackLayoutTests
{
    [Fact]
    public void DetectEdge_left_and_right()
    {
        Assert.Equal(ArHorizontalEdge.Left, ArEdgeStackLayout.DetectEdge(28f, 800f, 28f));
        Assert.Equal(ArHorizontalEdge.Right, ArEdgeStackLayout.DetectEdge(772f, 800f, 28f));
        Assert.Equal(ArHorizontalEdge.None, ArEdgeStackLayout.DetectEdge(400f, 800f, 28f));
    }

    [Fact]
    public void AssignStackedXs_left_edge_extreme_stays_outer()
    {
        // More left (bearing -0.4) should be at edge; closer-to-center (-0.1) inward.
        var keys = new[]
        {
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.4f),
        };
        var xs = new float[2];
        ArEdgeStackLayout.AssignStackedXs(ArHorizontalEdge.Left, 28f, 40f, keys, xs);
        Assert.Equal(28f + 40f, xs[0]); // inward
        Assert.Equal(28f, xs[1]); // outer
    }

    [Fact]
    public void AssignStackedXs_right_edge_extreme_stays_outer()
    {
        var keys = new[]
        {
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Right, 0.5f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Right, 0.1f),
        };
        var xs = new float[2];
        ArEdgeStackLayout.AssignStackedXs(ArHorizontalEdge.Right, 772f, 40f, keys, xs);
        Assert.Equal(772f, xs[0]); // outer
        Assert.Equal(772f - 40f, xs[1]); // inward
    }

    [Fact]
    public void AssignStackedXs_three_on_left_fan_inward()
    {
        var keys = new[]
        {
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.5f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.05f),
        };
        var xs = new float[3];
        ArEdgeStackLayout.AssignStackedXs(ArHorizontalEdge.Left, 28f, 40f, keys, xs);
        Assert.Equal(28f + 40f, xs[0]);
        Assert.Equal(28f, xs[1]);
        Assert.Equal(28f + 80f, xs[2]);
    }
}
