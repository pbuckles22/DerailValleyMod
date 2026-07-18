namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 consist / train-bar checks.
/// </summary>
public readonly struct ConsistDebugSnapshot
{
    public ConsistDebugSnapshot(bool hasLoco, string cars, string handbrakes)
    {
        HasLoco = hasLoco;
        Cars = cars;
        Handbrakes = handbrakes;
    }

    public bool HasLoco { get; }
    public string Cars { get; }
    public string Handbrakes { get; }

    public string FormatFragment() =>
        MonitorHudLine.Join(new[] { Cars, Handbrakes });

    public bool SameAs(ConsistDebugSnapshot other) =>
        HasLoco == other.HasLoco
        && Cars == other.Cars
        && Handbrakes == other.Handbrakes;
}

/// <summary>
/// Decides when to emit a Tier 2 consist debug line for Player.log.
/// </summary>
public static class Tier2ConsistDebug
{
    public const string Prefix = "T2 consist";

    public static string? NextLogMessage(ConsistDebugSnapshot? previous, ConsistDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init ({Where(current)}): {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.HasLoco != current.HasLoco)
        {
            return $"{Prefix} {Where(current)}: {current.FormatFragment()}";
        }

        if (!prior.SameAs(current))
        {
            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }

    private static string Where(ConsistDebugSnapshot snap) =>
        snap.HasLoco ? "loco" : "no-loco";
}
