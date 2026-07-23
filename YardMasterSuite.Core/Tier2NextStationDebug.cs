namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 fluid-gated next station (4.5).
/// </summary>
public readonly struct NextStationDebugSnapshot
{
    public NextStationDebugSnapshot(bool visible, string? label)
    {
        Visible = visible;
        Label = label;
    }

    public bool Visible { get; }
    public string? Label { get; }

    public string FormatFragment() =>
        Visible && !string.IsNullOrEmpty(Label) ? Label! : "— Next";
}

public static class Tier2NextStationDebug
{
    public const string Prefix = "T2 next-station";

    public static string? NextLogMessage(
        NextStationDebugSnapshot? previous,
        NextStationDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.Visible == current.Visible
            && string.Equals(prior.Label, current.Label, System.StringComparison.Ordinal))
        {
            return null;
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
