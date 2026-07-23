namespace YardMasterSuite.Core;

/// <summary>
/// Pure track-id formatting for the local-car HUD bar (4.4).
/// Bundle B.3: omit the segment when unknown / mainline / blank (no <c>— Track</c>).
/// </summary>
public static class TrackDisplay
{
    public static string? Format(string? trackId)
    {
        var id = trackId?.Trim();
        return string.IsNullOrEmpty(id) ? null : $"Track {id}";
    }
}
