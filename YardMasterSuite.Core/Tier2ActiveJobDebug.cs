namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 Active Job HUD (4.8).
/// </summary>
public readonly struct ActiveJobDebugSnapshot
{
    public ActiveJobDebugSnapshot(bool visible, string? jobId, string? bonusClock, string? zoneFragment)
    {
        Visible = visible;
        JobId = jobId;
        BonusClock = bonusClock;
        ZoneFragment = zoneFragment;
    }

    public bool Visible { get; }
    public string? JobId { get; }
    public string? BonusClock { get; }
    public string? ZoneFragment { get; }

    public string FormatFragment()
    {
        if (!Visible)
        {
            return "— JobHud";
        }

        return ActiveJobHudLine.Format(
            ActiveJobHudLine.FormatJobId(JobId, 0),
            BonusClock ?? "— Bonus",
            ZoneFragment ?? "— Zone");
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
                || prior.ZoneFragment != current.ZoneFragment))
        {
            // Bonus clock ticks often — only log when minute bucket or zone/job changes.
            var priorBucket = MinuteBucket(prior.BonusClock);
            var currentBucket = MinuteBucket(current.BonusClock);
            if (prior.JobId == current.JobId
                && prior.ZoneFragment == current.ZoneFragment
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
