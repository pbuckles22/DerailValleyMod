namespace YardMasterSuite.Core;

/// <summary>
/// Active-job summary bar (4.8): job id(s) · bonus clock · zone edge.
/// Null from the reader means the bar is omitted.
/// </summary>
public static class ActiveJobHudLine
{
    public static string Format(string job, string bonus, string zone) =>
        MonitorHudLine.Join(new[] { job, bonus, zone });

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
}
