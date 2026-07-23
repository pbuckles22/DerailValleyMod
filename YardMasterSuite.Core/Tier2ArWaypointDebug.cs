namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 AR wayfinding markers (4.9).
/// Quiet on meter tick — logs appear/hide and kind set changes.
/// </summary>
public readonly struct ArWaypointDebugSnapshot
{
    public ArWaypointDebugSnapshot(bool loco, bool station, bool pin)
    {
        Loco = loco;
        Station = station;
        Pin = pin;
    }

    public bool Loco { get; }
    public bool Station { get; }
    public bool Pin { get; }

    public string FormatFragment()
    {
        var parts = new System.Collections.Generic.List<string>(3);
        if (Loco)
        {
            parts.Add("loco");
        }

        if (Station)
        {
            parts.Add("office");
        }

        if (Pin)
        {
            parts.Add("pin");
        }

        return parts.Count == 0 ? "— AR" : string.Join("+", parts);
    }
}

public static class Tier2ArWaypointDebug
{
    public const string Prefix = "T2 ar";

    public static string? NextLogMessage(ArWaypointDebugSnapshot? previous, ArWaypointDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init: {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.Loco == current.Loco
            && prior.Station == current.Station
            && prior.Pin == current.Pin)
        {
            return null;
        }

        return $"{Prefix} change: {current.FormatFragment()}";
    }
}
