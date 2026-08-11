using System;
using System.IO;
using System.Linq;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Offline look at SW connective tissue from a dumped (or synthetic stand-in) snapshot.
/// Replace Fixtures/yardgraph_SW.txt with a real Dump graph capture when available.
/// </summary>
public class YardGraphFixtureDiagnosticTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "yardgraph_SW.txt");

    private static string ResolveFixturePath()
    {
        if (File.Exists(FixturePath))
        {
            return FixturePath;
        }

        // Fallback when content copy is not yet in output (source tree).
        var fromSource = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "YardMasterSuite.Tests", "Fixtures", "yardgraph_SW.txt"));
        if (File.Exists(fromSource))
        {
            return fromSource;
        }

        // Direct relative to cwd when running from repo root.
        var cwd = Path.GetFullPath(Path.Combine("YardMasterSuite.Tests", "Fixtures", "yardgraph_SW.txt"));
        return cwd;
    }

    [Fact]
    public void Smoke_SwTt_Fixture_PrintsJunctionChain_B4L_ToTt()
    {
        var path = ResolveFixturePath();
        Assert.True(File.Exists(path), "Missing fixture: " + path);

        var snap = YardGraphSnapshot.Parse(File.ReadAllText(path));
        Assert.Equal("SW", snap.YardId);
        Assert.Equal("SW-B4L", snap.OriginTrackId);
        Assert.False(string.IsNullOrWhiteSpace(snap.TurntableTrackId));

        var diag = snap.FormatJunctionChainDiagnostic("SW-B4L", snap.TurntableTrackId);
        Assert.Contains("origin=SW-B4L", diag);
        Assert.Contains("tt=", diag);

        // Neighborhood around TT — proves hop geometry is in the fixture.
        var nearTt = snap.CollectNeighborhood(snap.TurntableTrackId, maxHops: 3);
        Assert.NotEmpty(nearTt);

        // Visible in test output / CI logs — the point of this diagnostic.
        Assert.True(diag.Length > 20, diag);
    }

    [Fact]
    public void Smoke_SwTt_Fixture_FirstMisalignedJunction_IsApproachNotLead()
    {
        var path = ResolveFixturePath();
        Assert.True(File.Exists(path), "Missing fixture: " + path);

        var snap = YardGraphSnapshot.Parse(File.ReadAllText(path));
        var chain = snap.CollectJunctionChain("SW-B4L", snap.TurntableTrackId);

        // Real capture may differ; synthetic stand-in locks the intended product shape:
        // first misaligned junction is the approach switch, not the TT lead.
        if (snap.CapturedAt.Contains("synthetic", StringComparison.OrdinalIgnoreCase)
            || snap.CapturedAt == "synthetic-standin")
        {
            Assert.NotEmpty(chain);
            var firstFlip = chain.Find(c => c.RequiredBranch != c.SelectedBranch);
            Assert.Equal("J-approach", firstFlip.JunctionId);
            Assert.DoesNotContain(chain, c =>
                c.JunctionId == "J-lead" && c.RequiredBranch != c.SelectedBranch);
        }
        else
        {
            // Real dump: junction-first pin is the dual-branch approach (S-0421), not S-0220.
            var classMap = snap.Tracks.ToDictionary(t => t.TrackId, t => t.TrackClass, StringComparer.Ordinal);
            PathTrackClass ClassFor(string id) =>
                classMap.TryGetValue(id, out var c) ? c : PathTrackClass.Unknown;
            var selected = snap.Junctions.ToDictionary(
                j => j.JunctionId, j => j.SelectedBranch, StringComparer.Ordinal);
            string? YardFor(string id) => PathRouteConstraints.YardIdOf(id);
            var yard = PathPlan.Find(
                snap.Edges, selected, "SW-B4L", snap.TurntableTrackId, ClassFor,
                skipPlainOnMultiBranchStem: false, destYardId: "SW", yardFor: YardFor,
                mode: PathPlanMode.Yard);
            Assert.NotEqual(PathCheckStatus.NoPath, yard.Status);
            Assert.Equal("S-0421-SW", SwitchListRouteLeg.PickPinJunctionId(yard));
        }
    }
}
