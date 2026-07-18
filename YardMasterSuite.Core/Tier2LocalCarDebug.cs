namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 local-car second-bar checks.
/// </summary>
public readonly struct LocalCarDebugSnapshot
{
    public LocalCarDebugSnapshot(
        bool visible,
        string pipe,
        string handbrake,
        string coupling,
        string carNumber,
        string job)
    {
        Visible = visible;
        Pipe = pipe;
        Handbrake = handbrake;
        Coupling = coupling;
        CarNumber = carNumber;
        Job = job;
    }

    public bool Visible { get; }
    public string Pipe { get; }
    public string Handbrake { get; }
    public string Coupling { get; }
    public string CarNumber { get; }
    public string Job { get; }

    public string FormatFragment() =>
        MonitorHudLine.Join(new[] { Pipe, Handbrake, Coupling, CarNumber, Job });

    public bool SameAs(LocalCarDebugSnapshot other) =>
        Visible == other.Visible
        && Pipe == other.Pipe
        && Handbrake == other.Handbrake
        && Coupling == other.Coupling
        && CarNumber == other.CarNumber
        && Job == other.Job;
}

/// <summary>
/// Decides when to emit a Tier 2 local-car debug line for Player.log.
/// </summary>
public static class Tier2LocalCarDebug
{
    public const string Prefix = "T2 local-car";

    public static string? NextLogMessage(LocalCarDebugSnapshot? previous, LocalCarDebugSnapshot current)
    {
        if (previous is null)
        {
            return current.Visible
                ? $"{Prefix} init: {current.FormatFragment()}"
                : $"{Prefix} init (hidden)";
        }

        var prior = previous.Value;
        if (!prior.Visible && current.Visible)
        {
            return $"{Prefix} appear: {current.FormatFragment()}";
        }

        if (prior.Visible && !current.Visible)
        {
            return $"{Prefix} hide";
        }

        if (current.Visible && !prior.SameAs(current))
        {
            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }
}
