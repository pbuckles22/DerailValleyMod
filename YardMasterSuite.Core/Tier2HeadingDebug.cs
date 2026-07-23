namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 personal heading checks.
/// </summary>
public readonly struct HeadingDebugSnapshot
{
    public HeadingDebugSnapshot(string? compassPoint)
    {
        CompassPoint = compassPoint;
    }

    public string? CompassPoint { get; }

    public string FormatFragment() => HeadingDisplay.FormatPoint(CompassPoint);
}

public static class Tier2HeadingDebug
{
    public const string Prefix = "T2 heading";

    public static string? NextLogMessage(HeadingDebugSnapshot? previous, HeadingDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.CompassPoint == current.CompassPoint)
        {
            return null;
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
