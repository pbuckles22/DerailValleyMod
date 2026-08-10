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

    /// <summary>Look-around spam guard — change lines at most this often (seconds).</summary>
    public const float MinChangeLogSeconds = 2f;

    public static string? NextLogMessage(HeadingDebugSnapshot? previous, HeadingDebugSnapshot current) =>
        NextLogMessage(previous, current, nowSeconds: 0f, lastChangeLogAt: -999f, out _);

    public static string? NextLogMessage(
        HeadingDebugSnapshot? previous,
        HeadingDebugSnapshot current,
        float nowSeconds,
        float lastChangeLogAt,
        out float nextChangeLogAt)
    {
        nextChangeLogAt = lastChangeLogAt;
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.CompassPoint == current.CompassPoint)
        {
            return null;
        }

        if (nowSeconds - lastChangeLogAt < MinChangeLogSeconds)
        {
            return null;
        }

        nextChangeLogAt = nowSeconds;
        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
