namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 local-car second-bar checks.
/// </summary>
public readonly struct LocalCarDebugSnapshot
{
    public LocalCarDebugSnapshot(
        bool visible,
        string handbrake,
        string coupling,
        string? job,
        string? track,
        string? identityChip = null)
    {
        Visible = visible;
        Handbrake = handbrake;
        Coupling = coupling;
        Job = job;
        Track = track;
        IdentityChip = identityChip;
    }

    public bool Visible { get; }
    public string Handbrake { get; }
    public string Coupling { get; }
    public string? Job { get; }
    public string? Track { get; }
    public string? IdentityChip { get; }

    public string FormatFragment() =>
        LocalCarHudLine.Format(Handbrake, Coupling, Job, Track, IdentityChip);

    public bool SameAs(LocalCarDebugSnapshot other) =>
        Visible == other.Visible
        && Handbrake == other.Handbrake
        && Coupling == other.Coupling
        && Job == other.Job
        && Track == other.Track
        && IdentityChip == other.IdentityChip;
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

        if (prior.Visible && current.Visible && !prior.SameAs(current))
        {
            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }
}
