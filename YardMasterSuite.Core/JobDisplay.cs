namespace YardMasterSuite.Core;

/// <summary>
/// Pure job-id formatting for the local-car HUD bar.
/// Bundle B.4: omit the segment when no job (no <c>— Job</c>).
/// </summary>
public static class JobDisplay
{
    public static string? Format(string? jobId)
    {
        var id = jobId?.Trim();
        return string.IsNullOrEmpty(id) ? null : $"Job {id}";
    }
}
