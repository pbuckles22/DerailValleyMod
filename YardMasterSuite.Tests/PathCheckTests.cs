using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathCheckTests
{
    [Fact]
    public void Evaluate_no_destination_omits_check()
    {
        var result = PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            originTrackId: "A",
            destinationTrackId: null);

        Assert.Equal(PathCheckStatus.NoDestination, result.Status);
        Assert.Empty(result.TrackIds);
        Assert.Equal(0, result.MisalignedCount);
    }

    [Fact]
    public void Evaluate_no_origin_when_destination_set()
    {
        var result = PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            originTrackId: null,
            destinationTrackId: "B");

        Assert.Equal(PathCheckStatus.NoOrigin, result.Status);
    }

    [Fact]
    public void Evaluate_same_track_is_aligned()
    {
        var result = PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            "SM-A",
            "SM-A");

        Assert.Equal(PathCheckStatus.Aligned, result.Status);
        Assert.Equal(new[] { "SM-A" }, result.TrackIds);
        Assert.Empty(result.Junctions);
    }

    [Fact]
    public void Evaluate_no_path_when_disconnected()
    {
        var edges = new[]
        {
            new PathEdge("A", "B"),
            new PathEdge("B", "A"),
        };

        var result = PathCheck.Evaluate(edges, new Dictionary<string, int>(), "A", "Z");
        Assert.Equal(PathCheckStatus.NoPath, result.Status);
    }

    [Fact]
    public void Evaluate_aligned_when_junction_matches()
    {
        // Stem S ↔ branch B0 (branch 0) or B1 (branch 1)
        var edges = new[]
        {
            new PathEdge("S", "B0", "J1", 0),
            new PathEdge("B0", "S", "J1", 0),
            new PathEdge("S", "B1", "J1", 1),
            new PathEdge("B1", "S", "J1", 1),
        };
        var selected = new Dictionary<string, int> { ["J1"] = 0 };

        var result = PathCheck.Evaluate(edges, selected, "S", "B0");

        Assert.Equal(PathCheckStatus.Aligned, result.Status);
        Assert.Equal(new[] { "S", "B0" }, result.TrackIds);
        Assert.Single(result.Junctions);
        Assert.True(result.Junctions[0].Aligned);
        Assert.Equal(0, result.MisalignedCount);
    }

    [Fact]
    public void Evaluate_misaligned_when_wrong_branch_selected()
    {
        var edges = new[]
        {
            new PathEdge("S", "B0", "J1", 0),
            new PathEdge("B0", "S", "J1", 0),
            new PathEdge("S", "B1", "J1", 1),
            new PathEdge("B1", "S", "J1", 1),
        };
        var selected = new Dictionary<string, int> { ["J1"] = 1 };

        var result = PathCheck.Evaluate(edges, selected, "S", "B0");

        Assert.Equal(PathCheckStatus.Misaligned, result.Status);
        Assert.Equal(1, result.MisalignedCount);
        Assert.False(result.Junctions[0].Aligned);
        Assert.Equal(0, result.Junctions[0].RequiredBranch);
        Assert.Equal(1, result.Junctions[0].ActualBranch);
    }

    [Fact]
    public void Evaluate_plain_connection_needs_no_junction()
    {
        var edges = new[]
        {
            new PathEdge("A", "B"),
            new PathEdge("B", "A"),
            new PathEdge("B", "C"),
            new PathEdge("C", "B"),
        };

        var result = PathCheck.Evaluate(edges, new Dictionary<string, int>(), "A", "C");

        Assert.Equal(PathCheckStatus.Aligned, result.Status);
        Assert.Equal(new[] { "A", "B", "C" }, result.TrackIds);
        Assert.Empty(result.Junctions);
    }

    [Fact]
    public void Evaluate_counts_multiple_misaligned_junctions()
    {
        var edges = new[]
        {
            new PathEdge("A", "M", "J1", 0),
            new PathEdge("M", "A", "J1", 0),
            new PathEdge("M", "B", "J2", 1),
            new PathEdge("B", "M", "J2", 1),
        };
        var selected = new Dictionary<string, int>
        {
            ["J1"] = 1,
            ["J2"] = 0,
        };

        var result = PathCheck.Evaluate(edges, selected, "A", "B");

        Assert.Equal(PathCheckStatus.Misaligned, result.Status);
        Assert.Equal(2, result.MisalignedCount);
    }
}

public class PathCheckDisplayTests
{
    [Fact]
    public void Format_omits_when_no_destination()
    {
        var result = PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            "A",
            null);
        Assert.Null(PathCheckDisplay.Format(result));
    }

    [Fact]
    public void Format_aligned_and_misaligned()
    {
        var ok = new PathCheckResult(
            PathCheckStatus.Aligned,
            new[] { "A" },
            Array.Empty<PathJunctionEval>(),
            0);
        Assert.Equal("Path OK", PathCheckDisplay.Format(ok));

        var bad = new PathCheckResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B" },
            Array.Empty<PathJunctionEval>(),
            3);
        Assert.Equal("Path 3 switch", PathCheckDisplay.Format(bad));
    }

    [Fact]
    public void Format_no_path_and_no_origin()
    {
        Assert.Equal(
            "Path none",
            PathCheckDisplay.Format(new PathCheckResult(
                PathCheckStatus.NoPath,
                Array.Empty<string>(),
                Array.Empty<PathJunctionEval>(),
                0)));
        Assert.Equal(
            "Path —",
            PathCheckDisplay.Format(new PathCheckResult(
                PathCheckStatus.NoOrigin,
                Array.Empty<string>(),
                Array.Empty<PathJunctionEval>(),
                0)));
    }
}

[Collection("StaticSessions")]
public class PathCheckSessionTests
{
    public PathCheckSessionTests() => PathCheckSession.Clear();

    [Fact]
    public void Set_and_clear_destination()
    {
        Assert.False(PathCheckSession.HasDestination);
        PathCheckSession.SetDestination(" SM-O6I ");
        Assert.True(PathCheckSession.HasDestination);
        Assert.Equal("SM-O6I", PathCheckSession.DestinationTrackId);
        PathCheckSession.Clear();
        Assert.False(PathCheckSession.HasDestination);
        Assert.Null(PathCheckSession.DestinationTrackId);
    }

    [Fact]
    public void Set_rejects_blank()
    {
        PathCheckSession.SetDestination("  ");
        Assert.False(PathCheckSession.HasDestination);
    }
}
