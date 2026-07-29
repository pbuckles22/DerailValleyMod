namespace YardMasterSuite.Core;

/// <summary>
/// Session destination for Align Route (3.5): city/yard + track.
/// Look-at End pin may set track only.
/// </summary>
public static class RouteDestSession
{
    private static string? _yardId;
    private static string? _trackId;

    public static bool HasDestination => _trackId != null;

    public static string? YardId => _yardId;

    public static string? TrackId => _trackId;

    public static void Set(string? yardId, string? trackId)
    {
        var t = trackId?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            Clear();
            return;
        }

        _trackId = t;
        var y = yardId?.Trim();
        _yardId = string.IsNullOrEmpty(y) ? null : y;
    }

    public static void SetTrackOnly(string? trackId) => Set(null, trackId);

    public static void Clear()
    {
        _yardId = null;
        _trackId = null;
    }
}

/// <summary>
/// Align Route career gate — <c>GeneralLicenseType.Dispatcher1</c> (TRAIN DRIVER → Dispatcher).
/// </summary>
public static class RouteAlignAccess
{
    public static bool CanAlign(bool hasDispatcherLicense) => hasDispatcherLicense;

    public static string? DeniedChip(bool hasDispatcherLicense) =>
        hasDispatcherLicense ? null : "Need Dispatcher";
}
