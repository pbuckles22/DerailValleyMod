namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 park/return mark checks.
/// Logs on set/clear and 16-point return bearing changes — not every meter.
/// </summary>
public readonly struct ParkDebugSnapshot
{
    public ParkDebugSnapshot(bool hasMark, string? returnPoint)
    {
        HasMark = hasMark;
        ReturnPoint = returnPoint;
    }

    public bool HasMark { get; }

    /// <summary>16-point return bearing, <c>here</c>, or null when unmarked / unknown.</summary>
    public string? ReturnPoint { get; }

    public string FormatFragment()
    {
        if (!HasMark)
        {
            return "— Park";
        }

        return ReturnPoint is null ? "— Park" : $"Park {ReturnPoint}";
    }
}

public static class Tier2ParkDebug
{
    public const string Prefix = "T2 park";

    public static string? NextLogMessage(ParkDebugSnapshot? previous, ParkDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.HasMark == current.HasMark
            && string.Equals(prior.ReturnPoint, current.ReturnPoint, System.StringComparison.Ordinal))
        {
            return null;
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
