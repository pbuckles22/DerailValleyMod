using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Job AR pins: one per pickup track (car group), not per car.
/// Caption = job id · spur · count · distance.
/// </summary>
public static class JobCarMarkerDisplay
{
    /// <summary>Cap on simultaneous pickup-group pins.</summary>
    public const int DefaultMaxMarkers = 8;

    public static string? ShortSpurLabel(string? trackDisplay)
    {
        var t = trackDisplay?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return null;
        }

        // MF-C2S / HB-G3O → C2S / G3O (booklet-style spur). Keep #Y-* intact.
        var dash = t!.IndexOf('-');
        if (dash > 0 && dash <= 3 && dash < t.Length - 1 && t[0] != '#')
        {
            return t.Substring(dash + 1);
        }

        return t;
    }

    public static string FormatCaption(string? jobId, int carCount, float distanceMeters) =>
        FormatCaption(jobId, trackLabel: null, carCount, distanceMeters);

    public static string FormatCaption(
        string? jobId,
        string? trackLabel,
        int carCount,
        float distanceMeters)
    {
        var meters = (int)Math.Round(Math.Max(0f, distanceMeters), MidpointRounding.AwayFromZero);
        var id = jobId?.Trim();
        var track = trackLabel?.Trim();
        var n = carCount < 0 ? 0 : carCount;

        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(track))
        {
            return id + " · " + track + " · " + n + " " + meters + "m";
        }

        if (!string.IsNullOrEmpty(id))
        {
            return id + " · " + n + " " + meters + "m";
        }

        if (!string.IsNullOrEmpty(track))
        {
            return track + " · " + n + " " + meters + "m";
        }

        return n > 0 ? n + " cars " + meters + "m" : meters + "m";
    }

    /// <summary>
    /// Hide AR when a taken job's cars are all on the consist (GO).
    /// Backpack / held paperwork always shows while cars resolve.
    /// </summary>
    public static bool ShouldShowAr(bool jobTaken, JobConsistStatus status, int expectedCars)
    {
        if (expectedCars <= 0)
        {
            return false;
        }

        if (!jobTaken)
        {
            return true;
        }

        return status != JobConsistStatus.Ready;
    }
}
