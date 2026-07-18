namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 look-at second-bar checks (on foot).
/// Reuses <see cref="LocalCarDebugSnapshot"/> — same fragment fields as standing.
/// </summary>
public static class Tier2LookAtDebug
{
    public const string Prefix = "T2 look-at";

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
