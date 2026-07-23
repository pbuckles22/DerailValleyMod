namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 in-zone station waypoint (4.6).
/// </summary>
public readonly struct StationWaypointDebugSnapshot
{
    public StationWaypointDebugSnapshot(bool inZone, string? yardId, string? walkPoint)
    {
        InZone = inZone;
        YardId = yardId;
        WalkPoint = walkPoint;
    }

    public bool InZone { get; }
    public string? YardId { get; }

    /// <summary>16-point walk bearing, <c>here</c>, or null when out of zone / unknown.</summary>
    public string? WalkPoint { get; }

    public string FormatFragment()
    {
        if (!InZone)
        {
            return "— Station";
        }

        var label = string.IsNullOrWhiteSpace(YardId) ? "—" : YardId!.Trim();
        return WalkPoint is null ? $"Station {label}" : $"Station {label} {WalkPoint}";
    }
}

public static class Tier2StationWaypointDebug
{
    public const string Prefix = "T2 station";

    public static string? NextLogMessage(
        StationWaypointDebugSnapshot? previous,
        StationWaypointDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.InZone == current.InZone
            && string.Equals(prior.YardId, current.YardId, System.StringComparison.Ordinal)
            && string.Equals(prior.WalkPoint, current.WalkPoint, System.StringComparison.Ordinal))
        {
            return null;
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
