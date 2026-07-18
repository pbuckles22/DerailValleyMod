namespace YardMasterSuite.Core;

/// <summary>
/// Pure job-id formatting for the local-car HUD bar.
/// </summary>
public static class JobDisplay
{
    public static string Format(string? jobId)
    {
        var id = jobId?.Trim();
        return string.IsNullOrEmpty(id) ? "— Job" : $"Job {id}";
    }
}
