using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>One track node in a yard graph snapshot (offline replay of PathGraphBuilder).</summary>
public readonly struct YardGraphTrack
{
    public YardGraphTrack(
        string trackId,
        PathTrackClass trackClass,
        float lengthMeters,
        float? geometryLimitKmh,
        float worldX,
        float worldZ)
    {
        TrackId = trackId ?? string.Empty;
        TrackClass = trackClass;
        LengthMeters = lengthMeters;
        GeometryLimitKmh = geometryLimitKmh;
        WorldX = worldX;
        WorldZ = worldZ;
    }

    public string TrackId { get; }
    public PathTrackClass TrackClass { get; }
    public float LengthMeters { get; }
    public float? GeometryLimitKmh { get; }
    public float WorldX { get; }
    public float WorldZ { get; }
}

/// <summary>One junction pose + live selected branch at capture time.</summary>
public readonly struct YardGraphJunction
{
    public YardGraphJunction(string junctionId, float worldX, float worldZ, int selectedBranch)
    {
        JunctionId = junctionId ?? string.Empty;
        WorldX = worldX;
        WorldZ = worldZ;
        SelectedBranch = selectedBranch;
    }

    public string JunctionId { get; }
    public float WorldX { get; }
    public float WorldZ { get; }
    public int SelectedBranch { get; }
}

/// <summary>
/// Serializable yard graph for Tier 1 offline replay.
/// Line-based tab-separated records (no JSON) so net48 and tests share one parser.
/// </summary>
public sealed class YardGraphSnapshot
{
    public const int FormatVersion = 1;

    public string YardId { get; set; } = "";
    public string OriginTrackId { get; set; } = "";
    public string TurntableTrackId { get; set; } = "";
    public string CapturedAt { get; set; } = "";

    public List<YardGraphTrack> Tracks { get; } = new();
    public List<PathEdge> Edges { get; } = new();
    public List<YardGraphJunction> Junctions { get; } = new();
    public List<string> OccupiedTrackIds { get; } = new();

    public void Write(TextWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        writer.Write("V\t");
        writer.WriteLine(FormatVersion.ToString(CultureInfo.InvariantCulture));

        writer.Write("M\t");
        writer.Write(Escape(YardId));
        writer.Write('\t');
        writer.Write(Escape(OriginTrackId));
        writer.Write('\t');
        writer.Write(Escape(TurntableTrackId));
        writer.Write('\t');
        writer.WriteLine(Escape(CapturedAt));

        for (var i = 0; i < Tracks.Count; i++)
        {
            var t = Tracks[i];
            writer.Write("T\t");
            writer.Write(Escape(t.TrackId));
            writer.Write('\t');
            writer.Write(((int)t.TrackClass).ToString(CultureInfo.InvariantCulture));
            writer.Write('\t');
            writer.Write(Fmt(t.LengthMeters));
            writer.Write('\t');
            writer.Write(t.GeometryLimitKmh is float lim ? Fmt(lim) : "");
            writer.Write('\t');
            writer.Write(Fmt(t.WorldX));
            writer.Write('\t');
            writer.WriteLine(Fmt(t.WorldZ));
        }

        for (var i = 0; i < Edges.Count; i++)
        {
            var e = Edges[i];
            writer.Write("E\t");
            writer.Write(Escape(e.FromTrackId));
            writer.Write('\t');
            writer.Write(Escape(e.ToTrackId));
            writer.Write('\t');
            writer.Write(Fmt(e.Cost));
            writer.Write('\t');
            writer.Write(Escape(e.JunctionId ?? ""));
            writer.Write('\t');
            writer.Write(e.RequiredBranch.ToString(CultureInfo.InvariantCulture));
            writer.Write('\t');
            writer.WriteLine(e.RequiresReverse ? "1" : "0");
        }

        for (var i = 0; i < Junctions.Count; i++)
        {
            var j = Junctions[i];
            writer.Write("J\t");
            writer.Write(Escape(j.JunctionId));
            writer.Write('\t');
            writer.Write(Fmt(j.WorldX));
            writer.Write('\t');
            writer.Write(Fmt(j.WorldZ));
            writer.Write('\t');
            writer.WriteLine(j.SelectedBranch.ToString(CultureInfo.InvariantCulture));
        }

        for (var i = 0; i < OccupiedTrackIds.Count; i++)
        {
            writer.Write("O\t");
            writer.WriteLine(Escape(OccupiedTrackIds[i]));
        }
    }

    public string WriteToString()
    {
        var sb = new StringBuilder(4096);
        using (var sw = new StringWriter(sb, CultureInfo.InvariantCulture))
        {
            Write(sw);
        }

        return sb.ToString();
    }

    public static YardGraphSnapshot Parse(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var snap = new YardGraphSnapshot();
        using (var reader = new StringReader(text))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
                {
                    continue;
                }

                var parts = SplitTabs(line);
                if (parts.Length == 0)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "V":
                        // Version marker; ignore unknown future versions for now.
                        break;
                    case "M":
                        if (parts.Length >= 5)
                        {
                            snap.YardId = Unescape(parts[1]);
                            snap.OriginTrackId = Unescape(parts[2]);
                            snap.TurntableTrackId = Unescape(parts[3]);
                            snap.CapturedAt = Unescape(parts[4]);
                        }

                        break;
                    case "T":
                        if (parts.Length >= 7
                            && TryParseInt(parts[2], out var cls)
                            && TryParseFloat(parts[3], out var len)
                            && TryParseFloat(parts[5], out var wx)
                            && TryParseFloat(parts[6], out var wz))
                        {
                            float? lim = null;
                            if (!string.IsNullOrWhiteSpace(parts[4])
                                && TryParseFloat(parts[4], out var limVal))
                            {
                                lim = limVal;
                            }

                            snap.Tracks.Add(new YardGraphTrack(
                                Unescape(parts[1]),
                                (PathTrackClass)cls,
                                len,
                                lim,
                                wx,
                                wz));
                        }

                        break;
                    case "E":
                        if (parts.Length >= 7
                            && TryParseFloat(parts[3], out var cost)
                            && TryParseInt(parts[5], out var req)
                            && TryParseInt(parts[6], out var rev))
                        {
                            var jid = Unescape(parts[4]);
                            snap.Edges.Add(new PathEdge(
                                Unescape(parts[1]),
                                Unescape(parts[2]),
                                string.IsNullOrEmpty(jid) ? null : jid,
                                req,
                                cost,
                                rev != 0));
                        }

                        break;
                    case "J":
                        if (parts.Length >= 5
                            && TryParseFloat(parts[2], out var jx)
                            && TryParseFloat(parts[3], out var jz)
                            && TryParseInt(parts[4], out var sel))
                        {
                            snap.Junctions.Add(new YardGraphJunction(
                                Unescape(parts[1]), jx, jz, sel));
                        }

                        break;
                    case "O":
                        if (parts.Length >= 2)
                        {
                            var oid = Unescape(parts[1]);
                            if (!string.IsNullOrEmpty(oid))
                            {
                                snap.OccupiedTrackIds.Add(oid);
                            }
                        }

                        break;
                    // Malformed / unknown record kinds are skipped.
                }
            }
        }

        return snap;
    }

    /// <summary>
    /// Undirected BFS neighborhood of <paramref name="seedTrackId"/> (edge hops).
    /// </summary>
    public List<(string TrackId, int Hop)> CollectNeighborhood(string? seedTrackId, int maxHops)
    {
        var result = new List<(string, int)>();
        var seed = seedTrackId?.Trim();
        if (string.IsNullOrEmpty(seed) || maxHops < 0)
        {
            return result;
        }

        var adj = BuildUndirectedAdj();
        if (!adj.ContainsKey(seed!))
        {
            return result;
        }

        var best = new Dictionary<string, int>(StringComparer.Ordinal) { [seed!] = 0 };
        var q = new Queue<string>();
        q.Enqueue(seed!);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var hop = best[cur];
            if (hop >= maxHops || !adj.TryGetValue(cur, out var neighbors))
            {
                continue;
            }

            for (var i = 0; i < neighbors.Count; i++)
            {
                var n = neighbors[i];
                var next = hop + 1;
                if (best.TryGetValue(n, out var prev) && prev <= next)
                {
                    continue;
                }

                best[n] = next;
                q.Enqueue(n);
            }
        }

        foreach (var kv in best)
        {
            result.Add((kv.Key, kv.Value));
        }

        result.Sort((a, b) =>
        {
            var c = a.Item2.CompareTo(b.Item2);
            return c != 0 ? c : string.CompareOrdinal(a.Item1, b.Item1);
        });
        return result;
    }

    /// <summary>
    /// Directed corridor hops from origin toward turntable that carry a junction.
    /// Uses cheapest PathPlan corridor when reachable; otherwise empty.
    /// </summary>
    public List<(string From, string To, string JunctionId, int RequiredBranch, int SelectedBranch)>
        CollectJunctionChain(string? originTrackId, string? turntableTrackId)
    {
        var list = new List<(string, string, string, int, int)>();
        var origin = originTrackId?.Trim();
        var tt = turntableTrackId?.Trim();
        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(tt))
        {
            return list;
        }

        var classMap = new Dictionary<string, PathTrackClass>(StringComparer.Ordinal);
        for (var i = 0; i < Tracks.Count; i++)
        {
            classMap[Tracks[i].TrackId] = Tracks[i].TrackClass;
        }

        PathTrackClass ClassFor(string id) =>
            classMap.TryGetValue(id, out var c) ? c : PathTrackClass.Unknown;

        var selected = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < Junctions.Count; i++)
        {
            selected[Junctions[i].JunctionId] = Junctions[i].SelectedBranch;
        }

        var plan = PathPlan.Find(
            Edges, selected, origin!, tt!, ClassFor,
            skipPlainOnMultiBranchStem: false,
            destYardId: PathRouteConstraints.YardIdOf(origin) ?? PathRouteConstraints.YardIdOf(tt),
            mode: PathPlanMode.Yard);
        if (plan.Status == PathCheckStatus.NoPath
            || plan.Status == PathCheckStatus.NoOrigin
            || plan.Status == PathCheckStatus.NoDestination
            || plan.TrackIds.Count < 2)
        {
            return list;
        }

        var adj = BuildDirectedAdj();
        for (var i = 0; i < plan.TrackIds.Count - 1; i++)
        {
            var from = plan.TrackIds[i];
            var to = plan.TrackIds[i + 1];
            if (!adj.TryGetValue(from, out var outs))
            {
                continue;
            }

            for (var j = 0; j < outs.Count; j++)
            {
                var e = outs[j];
                if (!string.Equals(e.ToTrackId, to, StringComparison.Ordinal) || !e.HasJunction)
                {
                    continue;
                }

                selected.TryGetValue(e.JunctionId!, out var act);
                list.Add((from, to, e.JunctionId!, e.RequiredBranch, act));
                break;
            }
        }

        return list;
    }

    public string FormatJunctionChainDiagnostic(string? originTrackId = null, string? turntableTrackId = null)
    {
        var origin = originTrackId ?? OriginTrackId;
        var tt = turntableTrackId ?? TurntableTrackId;
        var chain = CollectJunctionChain(origin, tt);
        var sb = new StringBuilder();
        sb.Append("yard=");
        sb.Append(YardId);
        sb.Append(" origin=");
        sb.Append(origin);
        sb.Append(" tt=");
        sb.Append(tt);
        sb.Append(" tracks=");
        sb.Append(Tracks.Count);
        sb.Append(" edges=");
        sb.Append(Edges.Count);
        sb.Append(" junc=");
        sb.Append(Junctions.Count);
        sb.Append(" occ=");
        sb.Append(OccupiedTrackIds.Count);
        sb.Append(" chain=");
        sb.Append(chain.Count);
        if (chain.Count == 0)
        {
            sb.Append(" (empty — NoPath or no junction hops)");
            return sb.ToString();
        }

        for (var i = 0; i < chain.Count; i++)
        {
            var c = chain[i];
            sb.Append(" | ");
            sb.Append(c.From);
            sb.Append('→');
            sb.Append(c.To);
            sb.Append(" J=");
            sb.Append(c.JunctionId);
            sb.Append(' ');
            sb.Append(c.RequiredBranch);
            sb.Append('/');
            sb.Append(c.SelectedBranch);
            if (c.RequiredBranch != c.SelectedBranch)
            {
                sb.Append('!');
            }
        }

        return sb.ToString();
    }

    private Dictionary<string, List<string>> BuildUndirectedAdj()
    {
        var adj = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void Link(string a, string b)
        {
            if (!adj.TryGetValue(a, out var list))
            {
                list = new List<string>(4);
                adj[a] = list;
            }

            for (var i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], b, StringComparison.Ordinal))
                {
                    return;
                }
            }

            list.Add(b);
        }

        for (var i = 0; i < Edges.Count; i++)
        {
            var from = Edges[i].FromTrackId?.Trim();
            var to = Edges[i].ToTrackId?.Trim();
            if (string.IsNullOrEmpty(from)
                || string.IsNullOrEmpty(to)
                || string.Equals(from, to, StringComparison.Ordinal))
            {
                continue;
            }

            Link(from!, to!);
            Link(to!, from!);
        }

        return adj;
    }

    private Dictionary<string, List<PathEdge>> BuildDirectedAdj()
    {
        var adj = new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
        for (var i = 0; i < Edges.Count; i++)
        {
            var from = Edges[i].FromTrackId?.Trim();
            if (string.IsNullOrEmpty(from))
            {
                continue;
            }

            if (!adj.TryGetValue(from!, out var list))
            {
                list = new List<PathEdge>(4);
                adj[from!] = list;
            }

            list.Add(Edges[i]);
        }

        return adj;
    }

    private static string[] SplitTabs(string line)
    {
        return line.Split('\t');
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value!
            .Replace("\\", "\\\\")
            .Replace("\t", "\\t")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private static string Unescape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var s = value!;
        var sb = new StringBuilder(s.Length);
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                var n = s[i + 1];
                sb.Append(n switch
                {
                    't' => '\t',
                    'n' => '\n',
                    'r' => '\r',
                    '\\' => '\\',
                    _ => n,
                });
                i++;
                continue;
            }

            sb.Append(s[i]);
        }

        return sb.ToString();
    }

    private static string Fmt(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static bool TryParseFloat(string? s, out float value) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static bool TryParseInt(string? s, out int value) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
