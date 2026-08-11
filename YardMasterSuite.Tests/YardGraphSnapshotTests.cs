using System.IO;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class YardGraphSnapshotTests
{
    private static YardGraphSnapshot SampleSwSlice()
    {
        var snap = new YardGraphSnapshot
        {
            YardId = "SW",
            OriginTrackId = "SW-B4L",
            TurntableTrackId = "#Y-#S1774#T",
            CapturedAt = "2026-08-10T00:00:00Z",
        };

        snap.Tracks.Add(new YardGraphTrack("SW-B4L", PathTrackClass.YardService, 40f, 25f, 0f, 0f));
        snap.Tracks.Add(new YardGraphTrack("SW-A2P", PathTrackClass.YardService, 35f, 25f, 10f, 80f));
        snap.Tracks.Add(new YardGraphTrack("#Y-throat", PathTrackClass.Unknown, 20f, null, 20f, 140f));
        snap.Tracks.Add(new YardGraphTrack("SW-T11P", PathTrackClass.YardService, 25f, 25f, 30f, 180f));
        snap.Tracks.Add(new YardGraphTrack("#Y-#S1774#T", PathTrackClass.Unknown, 15f, null, 40f, 220f));

        // B4L --J1--> A2P --J2--> throat --J3--> T11P --plain--> TT
        snap.Edges.Add(new PathEdge("SW-B4L", "SW-A2P", "J-approach", requiredBranch: 1, cost: 4f));
        snap.Edges.Add(new PathEdge("SW-A2P", "SW-B4L", "J-approach", requiredBranch: 0, cost: 4f));
        snap.Edges.Add(new PathEdge("SW-A2P", "#Y-throat", "J-throat", requiredBranch: 1, cost: 3f));
        snap.Edges.Add(new PathEdge("#Y-throat", "SW-A2P", "J-throat", requiredBranch: 0, cost: 3f));
        snap.Edges.Add(new PathEdge("#Y-throat", "SW-T11P", "J-lead", requiredBranch: 1, cost: 2f));
        snap.Edges.Add(new PathEdge("SW-T11P", "#Y-throat", "J-lead", requiredBranch: 0, cost: 2f));
        snap.Edges.Add(new PathEdge("SW-T11P", "#Y-#S1774#T", cost: 1.5f));
        snap.Edges.Add(new PathEdge("#Y-#S1774#T", "SW-T11P", cost: 1.5f));

        // Misaligned approach switch (stop & flip); lead already correct.
        snap.Junctions.Add(new YardGraphJunction("J-approach", 8f, 40f, selectedBranch: 0));
        snap.Junctions.Add(new YardGraphJunction("J-throat", 18f, 120f, selectedBranch: 1));
        snap.Junctions.Add(new YardGraphJunction("J-lead", 28f, 160f, selectedBranch: 1));

        snap.OccupiedTrackIds.Add("SW-C1O");
        return snap;
    }

    [Fact]
    public void RoundTrip_PreservesTracksEdgesJunctionsOccupancy()
    {
        var original = SampleSwSlice();
        var text = original.WriteToString();
        var parsed = YardGraphSnapshot.Parse(text);

        Assert.Equal("SW", parsed.YardId);
        Assert.Equal("SW-B4L", parsed.OriginTrackId);
        Assert.Equal("#Y-#S1774#T", parsed.TurntableTrackId);
        Assert.Equal(original.CapturedAt, parsed.CapturedAt);

        Assert.Equal(5, parsed.Tracks.Count);
        Assert.Equal("SW-A2P", parsed.Tracks[1].TrackId);
        Assert.Equal(PathTrackClass.YardService, parsed.Tracks[1].TrackClass);
        Assert.Equal(35f, parsed.Tracks[1].LengthMeters);
        Assert.Equal(25f, parsed.Tracks[1].GeometryLimitKmh);
        Assert.Equal(10f, parsed.Tracks[1].WorldX);
        Assert.Equal(80f, parsed.Tracks[1].WorldZ);

        Assert.Equal(8, parsed.Edges.Count);
        var approach = parsed.Edges[0];
        Assert.Equal("SW-B4L", approach.FromTrackId);
        Assert.Equal("SW-A2P", approach.ToTrackId);
        Assert.Equal("J-approach", approach.JunctionId);
        Assert.Equal(1, approach.RequiredBranch);
        Assert.False(approach.RequiresReverse);

        Assert.Equal(3, parsed.Junctions.Count);
        Assert.Equal("J-approach", parsed.Junctions[0].JunctionId);
        Assert.Equal(0, parsed.Junctions[0].SelectedBranch);

        Assert.Single(parsed.OccupiedTrackIds);
        Assert.Equal("SW-C1O", parsed.OccupiedTrackIds[0]);
    }

    [Fact]
    public void Parse_SkipsMalformedAndUnknownRecords()
    {
        const string text = """
            V	1
            M	SW	SW-B4L	#Y-TT	now
            # comment
            T	too	few
            T	SW-B4L	2	40		0	0
            E	SW-B4L	SW-A2P	not-a-float	J1	1	0
            E	SW-B4L	SW-A2P	4	J1	1	0
            J	J1	1	2	0
            O
            O	SW-C1O
            X	ignored
            """;

        var snap = YardGraphSnapshot.Parse(text);
        Assert.Equal("SW", snap.YardId);
        Assert.Single(snap.Tracks);
        Assert.Single(snap.Edges);
        Assert.Single(snap.Junctions);
        Assert.Single(snap.OccupiedTrackIds);
    }

    [Fact]
    public void CollectJunctionChain_MarksMisalignedApproach()
    {
        var snap = SampleSwSlice();
        var chain = snap.CollectJunctionChain(snap.OriginTrackId, snap.TurntableTrackId);
        Assert.NotEmpty(chain);
        Assert.Equal("J-approach", chain[0].JunctionId);
        Assert.Equal(1, chain[0].RequiredBranch);
        Assert.Equal(0, chain[0].SelectedBranch);

        var diag = snap.FormatJunctionChainDiagnostic();
        Assert.Contains("J-approach 1/0!", diag);
        Assert.Contains("origin=SW-B4L", diag);
    }

    [Fact]
    public void CollectNeighborhood_OrdersByHopThenId()
    {
        var snap = SampleSwSlice();
        var near = snap.CollectNeighborhood("#Y-#S1774#T", maxHops: 2);
        Assert.Contains(near, row => row.TrackId == "#Y-#S1774#T" && row.Hop == 0);
        Assert.Contains(near, row => row.TrackId == "SW-T11P" && row.Hop == 1);
        Assert.Contains(near, row => row.TrackId == "#Y-throat" && row.Hop == 2);
    }

    [Fact]
    public void Write_ThenParse_ViaTempFile()
    {
        var original = SampleSwSlice();
        var path = Path.Combine(Path.GetTempPath(), "yardgraph_roundtrip_" + Path.GetRandomFileName() + ".txt");
        try
        {
            File.WriteAllText(path, original.WriteToString());
            var parsed = YardGraphSnapshot.Parse(File.ReadAllText(path));
            Assert.Equal(original.Edges.Count, parsed.Edges.Count);
            Assert.Equal(original.Junctions.Count, parsed.Junctions.Count);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
