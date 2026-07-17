namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 integrity checks (no per-frame spam).
/// </summary>
public readonly struct IntegrityDebugSnapshot
{
    public IntegrityDebugSnapshot(bool onCar, string pipe, string handbrake, string coupling)
    {
        OnCar = onCar;
        Pipe = pipe;
        Handbrake = handbrake;
        Coupling = coupling;
    }

    public bool OnCar { get; }
    public string Pipe { get; }
    public string Handbrake { get; }
    public string Coupling { get; }

    public string FormatFragment() =>
        MonitorHudLine.Join(new[] { Pipe, Handbrake, Coupling });

    public bool SameAs(IntegrityDebugSnapshot other) =>
        OnCar == other.OnCar
        && Pipe == other.Pipe
        && Handbrake == other.Handbrake
        && Coupling == other.Coupling;
}

/// <summary>
/// Decides when to emit a Tier 2 integrity debug line for Player.log.
/// </summary>
public static class Tier2IntegrityDebug
{
    public const string Prefix = "T2 integrity";

    /// <summary>
    /// Returns a log line when the snapshot is new or meaningful for sign-off; otherwise null.
    /// </summary>
    public static string? NextLogMessage(IntegrityDebugSnapshot? previous, IntegrityDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init ({Where(current)}): {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.OnCar != current.OnCar)
        {
            return $"{Prefix} {Where(current)}: {current.FormatFragment()}";
        }

        if (!prior.SameAs(current))
        {
            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }

    private static string Where(IntegrityDebugSnapshot snap) =>
        snap.OnCar ? "on-car" : "on-foot";
}
