namespace YardMasterSuite.Core;

/// <summary>
/// Pure track-id formatting for the local-car HUD bar (4.4).
/// </summary>
public static class TrackDisplay
{
    public static string Format(string? trackId)
    {
        var id = trackId?.Trim();
        return string.IsNullOrEmpty(id) ? "— Track" : $"Track {id}";
    }
}
