namespace YardMasterSuite.Core;

/// <summary>
/// Active-job summary bar (4.8 / Bundle D):
/// taken = Job + GO/HOLD/RED + Bonus; Cancelled flash;
/// not taken (backpack) = license warn + optional Preview edge (no job id / GO chip).
/// Null from the reader means the bar is omitted.
/// </summary>
public static class ActiveJobHudLine
{
    public const string CancelledColor = "#FF5555";

    /// <summary>Taken job: id · GO/HOLD/RED · bonus.</summary>
    public static string Format(string job, string status, string bonus) =>
        MonitorHudLine.Join(new[] { job, status, bonus });

    /// <summary>Taken job without status chip (tests / cancelled paths).</summary>
    public static string Format(string job, string bonus) =>
        MonitorHudLine.Join(new[] { job, bonus });

    public static string FormatPreview(string previewChip) => previewChip.Trim();

    /// <summary>
    /// Pre-validate prep bar: optional license warn + optional Preview edge.
    /// Null when both omitted.
    /// </summary>
    public static string? FormatPrep(string? licenseWarn, string? previewChip)
    {
        var joined = MonitorHudLine.Join(new[] { licenseWarn ?? "", previewChip ?? "" });
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    public static string FormatCancelled(string? jobId, bool richText = false)
    {
        var id = jobId?.Trim();
        var text = string.IsNullOrEmpty(id)
            ? "Cancelled"
            : MonitorHudLine.Join(new[] { $"Job {id}", "Cancelled" });

        if (!richText)
        {
            return text;
        }

        return $"<color={CancelledColor}>{text}</color>";
    }

    public static string FormatJobId(string? primaryJobId, int extraJobCount)
    {
        var id = primaryJobId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return "— Job";
        }

        if (extraJobCount <= 0)
        {
            return $"Job {id}";
        }

        return $"Job {id} (+{extraJobCount})";
    }

    /// <summary>True for DV JobState names that should flash Cancelled (not Failed/Completed).</summary>
    public static bool IsCancelledState(string? jobStateName) =>
        jobStateName == "Abandoned" || jobStateName == "Expired";
}
