namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 coupler tight/loose checks.
/// </summary>
public readonly struct CouplerDebugSnapshot
{
    public CouplerDebugSnapshot(bool visible, string coupling)
    {
        Visible = visible;
        Coupling = coupling;
    }

    public bool Visible { get; }
    public string Coupling { get; }

    public bool SameAs(CouplerDebugSnapshot other) =>
        Visible == other.Visible && Coupling == other.Coupling;
}

/// <summary>
/// Decides when to emit a Tier 2 coupler debug line for Player.log.
/// </summary>
public static class Tier2CouplerDebug
{
    public const string Prefix = "T2 coupler";

    public static string? NextLogMessage(CouplerDebugSnapshot? previous, CouplerDebugSnapshot current)
    {
        if (previous is null)
        {
            return current.Visible
                ? $"{Prefix} init: {current.Coupling}"
                : $"{Prefix} init (hidden)";
        }

        var prior = previous.Value;
        if (!prior.Visible && current.Visible)
        {
            return $"{Prefix} appear: {current.Coupling}";
        }

        if (prior.Visible && !current.Visible)
        {
            return $"{Prefix} hide";
        }

        if (current.Visible && !prior.SameAs(current))
        {
            return $"{Prefix} change: {current.Coupling}";
        }

        return null;
    }
}
