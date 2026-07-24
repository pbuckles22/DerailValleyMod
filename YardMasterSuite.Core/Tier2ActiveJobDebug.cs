namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 Active Job HUD (4.8 / Bundle D).
/// </summary>
public readonly struct ActiveJobDebugSnapshot
{
    public ActiveJobDebugSnapshot(
        bool visible,
        string? jobId,
        string? bonusClock,
        string? previewFragment,
        string? licenseWarnFragment = null)
    {
        Visible = visible;
        JobId = jobId;
        BonusClock = bonusClock;
        PreviewFragment = previewFragment;
        LicenseWarnFragment = licenseWarnFragment;
    }

    public bool Visible { get; }
    public string? JobId { get; }
    public string? BonusClock { get; }
    /// <summary>Preview edge chip, or null when taken job / cancelled / hidden.</summary>
    public string? PreviewFragment { get; }
    /// <summary>Held-overview missing-license chip (plain text), or null.</summary>
    public string? LicenseWarnFragment { get; }

    public string FormatFragment()
    {
        if (!Visible)
        {
            return "— JobHud";
        }

        if (string.IsNullOrEmpty(JobId) && string.IsNullOrEmpty(BonusClock)
            && (!string.IsNullOrEmpty(PreviewFragment) || !string.IsNullOrEmpty(LicenseWarnFragment)))
        {
            return ActiveJobHudLine.FormatPrep(LicenseWarnFragment, PreviewFragment) ?? "— JobHud";
        }

        if (BonusClock == "Cancelled" || BonusClock?.Contains("Cancelled") == true)
        {
            return ActiveJobHudLine.FormatCancelled(JobId);
        }

        return ActiveJobHudLine.Format(
            ActiveJobHudLine.FormatJobId(JobId, 0),
            BonusClock ?? "— Bonus");
    }
}

public static class Tier2ActiveJobDebug
{
    public const string Prefix = "T2 job";

    public static string? NextLogMessage(ActiveJobDebugSnapshot? previous, ActiveJobDebugSnapshot current)
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

        if (current.Visible
            && (prior.JobId != current.JobId
                || prior.BonusClock != current.BonusClock
                || prior.PreviewFragment != current.PreviewFragment
                || prior.LicenseWarnFragment != current.LicenseWarnFragment))
        {
            // Bonus clock ticks often — only log when minute bucket or preview/job/license changes.
            var priorBucket = MinuteBucket(prior.BonusClock);
            var currentBucket = MinuteBucket(current.BonusClock);
            if (prior.JobId == current.JobId
                && prior.PreviewFragment == current.PreviewFragment
                && prior.LicenseWarnFragment == current.LicenseWarnFragment
                && priorBucket == currentBucket)
            {
                return null;
            }

            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }

    private static string? MinuteBucket(string? bonusClock)
    {
        if (string.IsNullOrEmpty(bonusClock))
        {
            return bonusClock;
        }

        // "Bonus 14:32" / "Bonus 1:02:03" → drop seconds for quiet logs.
        var text = bonusClock!;
        var colon = text.LastIndexOf(':');
        return colon > 0 ? text.Substring(0, colon) : text;
    }
}
