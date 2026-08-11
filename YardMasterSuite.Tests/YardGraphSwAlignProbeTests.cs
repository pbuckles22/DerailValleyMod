using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Offline probes against the captured SW yardgraph fixture.
/// Locks World vs Yard PathPlan profiles against real dump connective tissue.
/// </summary>
public class YardGraphSwAlignProbeTests
{
    private static YardGraphSnapshot Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "yardgraph_SW.txt");
        if (!File.Exists(path))
        {
            path = Path.GetFullPath(Path.Combine("YardMasterSuite.Tests", "Fixtures", "yardgraph_SW.txt"));
        }

        Assert.True(File.Exists(path), "Missing fixture: " + path);
        return YardGraphSnapshot.Parse(File.ReadAllText(path));
    }

    /// <summary>
    /// Capture truth: directed topology connects; World Dijkstra still NoPath;
    /// Yard profile finds the TT corridor.
    /// </summary>
    [Fact]
    public void Smoke_SwB4L_ToTt_WorldNoPath_YardFindsPath()
    {
        var snap = Load();
        var origin = "SW-B4L";
        var tt = snap.TurntableTrackId;
        Assert.False(string.IsNullOrWhiteSpace(tt));

        var classMap = snap.Tracks.ToDictionary(t => t.TrackId, t => t.TrackClass, StringComparer.Ordinal);
        PathTrackClass ClassFor(string id) =>
            classMap.TryGetValue(id, out var c) ? c : PathTrackClass.Unknown;

        var selected = snap.Junctions.ToDictionary(
            j => j.JunctionId, j => j.SelectedBranch, StringComparer.Ordinal);

        string? YardFor(string id) => PathRouteConstraints.YardIdOf(id);

        var bfs = DirectedBfs(snap.Edges, origin, tt!);
        Assert.True(bfs.Count >= 2, "directed BFS must reach TT");

        var world = PathPlan.Find(
            snap.Edges, selected, origin, tt, ClassFor,
            skipPlainOnMultiBranchStem: false, destYardId: "SW", yardFor: YardFor,
            mode: PathPlanMode.World);
        Assert.Equal(PathCheckStatus.NoPath, world.Status);

        var yard = PathPlan.Find(
            snap.Edges, selected, origin, tt, ClassFor,
            skipPlainOnMultiBranchStem: false, destYardId: "SW", yardFor: YardFor,
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, yard.Status);
        Assert.True(yard.TrackIds.Count >= 2);
        Assert.Equal(tt, yard.TrackIds[yard.TrackIds.Count - 1]);
        Assert.Equal(origin, yard.TrackIds[0]);
    }

    /// <summary>
    /// Live occupancy filter must not seal the free B4L→TT corridor on this dump.
    /// </summary>
    [Fact]
    public void Smoke_SwB4L_ToTt_YardFindsPath_AfterOccupancyFilter()
    {
        var snap = Load();
        var origin = "SW-B4L";
        var tt = snap.TurntableTrackId!;

        var classMap = snap.Tracks.ToDictionary(t => t.TrackId, t => t.TrackClass, StringComparer.Ordinal);
        PathTrackClass ClassFor(string id) =>
            classMap.TryGetValue(id, out var c) ? c : PathTrackClass.Unknown;

        var selected = snap.Junctions.ToDictionary(
            j => j.JunctionId, j => j.SelectedBranch, StringComparer.Ordinal);
        string? YardFor(string id) => PathRouteConstraints.YardIdOf(id);

        var occupied = PathRouteConstraints.OccupiedSet(snap.OccupiedTrackIds);
        Assert.Contains("SW-A1P", occupied);
        var expanded = PathRouteConstraints.ExpandOccupiedThroughAnonymous(
            occupied, snap.Edges, origin, tt);
        var filtered = PathRouteConstraints.FilterEdges(
            snap.Edges, ClassFor, expanded, origin, tt, YardFor, "SW");
        Assert.True(filtered.Count > 0);
        Assert.True(filtered.Count < snap.Edges.Count);

        var yard = PathPlan.Find(
            filtered, selected, origin, tt, ClassFor,
            skipPlainOnMultiBranchStem: false, destYardId: "SW", yardFor: YardFor,
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, yard.Status);
        Assert.Equal(tt, yard.TrackIds[yard.TrackIds.Count - 1]);
    }

    private static List<string> DirectedBfs(IReadOnlyList<PathEdge> edges, string origin, string dest)
    {
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var e in edges)
        {
            if (!adj.TryGetValue(e.FromTrackId, out var list))
            {
                list = new List<string>();
                adj[e.FromTrackId] = list;
            }

            list.Add(e.ToTrackId);
        }

        var parent = new Dictionary<string, string>(StringComparer.Ordinal);
        var q = new Queue<string>();
        q.Enqueue(origin);
        var seen = new HashSet<string>(StringComparer.Ordinal) { origin };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == dest)
            {
                break;
            }

            if (!adj.TryGetValue(cur, out var outs))
            {
                continue;
            }

            foreach (var n in outs)
            {
                if (seen.Add(n))
                {
                    parent[n] = cur;
                    q.Enqueue(n);
                }
            }
        }

        if (!seen.Contains(dest))
        {
            return new List<string>();
        }

        var path = new List<string> { dest };
        for (var n = dest; parent.TryGetValue(n, out var p); n = p)
        {
            path.Add(p);
        }

        path.Reverse();
        return path;
    }
}
