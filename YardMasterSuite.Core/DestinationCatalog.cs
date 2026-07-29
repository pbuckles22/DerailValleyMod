using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Group track catalog entries by yard/city for destination pickers (3.5).</summary>
public static class DestinationCatalog
{
    public static IReadOnlyList<string> ListYards(IEnumerable<(string YardId, string TrackId)> entries)
    {
        var yards = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (entries == null)
        {
            return Array.Empty<string>();
        }

        foreach (var (yardId, trackId) in entries)
        {
            var y = yardId?.Trim();
            var t = trackId?.Trim();
            if (string.IsNullOrEmpty(y) || string.IsNullOrEmpty(t))
            {
                continue;
            }

            yards.Add(y!);
        }

        return new List<string>(yards);
    }

    public static IReadOnlyList<string> ListTracksInYard(
        IEnumerable<(string YardId, string TrackId)> entries,
        string? yardId)
    {
        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard) || entries == null)
        {
            return Array.Empty<string>();
        }

        var tracks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (y, trackId) in entries)
        {
            if (!string.Equals(y?.Trim(), yard, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var t = trackId?.Trim();
            if (!string.IsNullOrEmpty(t))
            {
                tracks.Add(t!);
            }
        }

        return new List<string>(tracks);
    }

    public static int CycleIndex(int current, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        var next = current + delta;
        next %= count;
        if (next < 0)
        {
            next += count;
        }

        return next;
    }
}
