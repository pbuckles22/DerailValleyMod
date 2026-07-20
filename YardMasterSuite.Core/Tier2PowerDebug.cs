namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 power-monitor checks (Load / Motors / Fuel / Oil).
/// </summary>
public readonly struct PowerDebugSnapshot
{
    public PowerDebugSnapshot(bool hasLoco, string load, string motors, string fuel, string oil)
    {
        HasLoco = hasLoco;
        Load = load;
        Motors = motors;
        Fuel = fuel;
        Oil = oil;
    }

    public bool HasLoco { get; }
    public string Load { get; }
    public string Motors { get; }
    public string Fuel { get; }
    public string Oil { get; }

    public string FormatFragment() => $"{Load}  |  {Motors}  |  {Fuel}  |  {Oil}";

    public bool SameAs(PowerDebugSnapshot other) =>
        HasLoco == other.HasLoco
        && Load == other.Load
        && Motors == other.Motors
        && Fuel == other.Fuel
        && Oil == other.Oil;
}

/// <summary>
/// Decides when to emit a Tier 2 power debug line for Player.log.
/// </summary>
public static class Tier2PowerDebug
{
    public const string Prefix = "T2 power";

    public static string? NextLogMessage(PowerDebugSnapshot? previous, PowerDebugSnapshot current)
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

    private static string Where(PowerDebugSnapshot snap) =>
        snap.HasLoco ? "loco" : "no-loco";
}
