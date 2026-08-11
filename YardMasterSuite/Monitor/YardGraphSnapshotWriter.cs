using System;
using System.IO;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Writes a <see cref="YardGraphSnapshot"/> under Mods/YardMasterSuite/dumps for offline Tier 1 replay.
/// </summary>
internal static class YardGraphSnapshotWriter
{
    /// <summary>
    /// Capture raw graph (even when Dijkstra is occupancy-sealed) and write a yardgraph_*.txt file.
    /// Returns the absolute path on success; null with <paramref name="error"/> on failure.
    /// </summary>
    public static string? TryDump(
        string? yardId,
        string? originTrackId,
        string? turntableTrackId,
        out string error)
    {
        error = "";
        if (!PathGraphBuilder.HasReadyCache)
        {
            error = "graph not ready (map first)";
            return null;
        }

        var origin = originTrackId?.Trim();
        if (string.IsNullOrEmpty(origin))
        {
            origin = TelemetryReaderOrigin.TryGet();
        }

        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard) && !string.IsNullOrEmpty(origin))
        {
            yard = PathRouteConstraints.YardIdOf(origin);
        }

        var tt = turntableTrackId?.Trim();
        if (string.IsNullOrEmpty(tt) && !string.IsNullOrEmpty(yard))
        {
            tt = TryResolveTurntable(yard!);
        }

        var occupied = PathOccupancyScanner.SnapshotOccupiedTrackKeys();
        if (!PathGraphBuilder.TryCaptureSnapshot(yard, origin, tt, occupied, out var snap))
        {
            error = "capture empty (no edges/tracks)";
            return null;
        }

        var dir = DumpsDirectory();
        if (string.IsNullOrEmpty(dir))
        {
            error = "no mod path for dumps/";
            return null;
        }

        try
        {
            Directory.CreateDirectory(dir!);
            var yardTag = SanitizeFileToken(string.IsNullOrEmpty(yard) ? "yard" : yard!);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var path = Path.Combine(dir!, "yardgraph_" + yardTag + "_" + stamp + ".txt");
            File.WriteAllText(path, snap.WriteToString());
            Main.Log(
                "T2 graph: dump "
                + path
                + " tracks="
                + snap.Tracks.Count
                + " edges="
                + snap.Edges.Count
                + " junc="
                + snap.Junctions.Count
                + " occ="
                + snap.OccupiedTrackIds.Count
                + " origin="
                + (origin ?? "—")
                + " tt="
                + (tt ?? "—"));
            return path;
        }
        catch (Exception ex)
        {
            error = "write failed: " + ex.Message;
            Main.Log("T2 graph: dump failed · " + error);
            return null;
        }
    }

    private static string? TryResolveTurntable(string yard)
    {
        try
        {
            if (!PathGraphBuilder.TryGetTrackWorldXZ(TelemetryReaderOrigin.TryGet(), out var ox, out var oz))
            {
                ox = oz = 0f;
            }

            return TurntableLocator.TryResolveTrackId(yard, ox, oz);
        }
        catch
        {
            return null;
        }
    }

    private static string? DumpsDirectory()
    {
        var icons = Main.IconsPath;
        if (string.IsNullOrEmpty(icons))
        {
            return null;
        }

        var root = Path.GetDirectoryName(icons);
        return string.IsNullOrEmpty(root) ? null : Path.Combine(root!, "dumps");
    }

    private static string SanitizeFileToken(string raw)
    {
        var chars = raw.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
