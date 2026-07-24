namespace YardMasterSuite.Core;

/// <summary>
/// Active-job summary bar (4.8 / Bundle D):
/// taken = job + bonus only; preview/prep = Preview edge; cancelled flash.
/// Null from the reader means the bar is omitted.
/// </summary>
public static class ActiveJobHudLine
{
    public const string CancelledColor = "#FF5555";

    /// <summary>Taken job: id + bonus only (no distance — validated jobs are not wiped by Regular edge).</summary>
    public static string Format(string job, string bonus) =>
        MonitorHudLine.Join(new[] { job, bonus });

    public static string FormatPreview(string previewChip) => previewChip.Trim();

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
