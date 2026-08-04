namespace YardMasterSuite.Core;

/// <summary>Active place-mode session for job-cars teleport (3.1).</summary>
public static class JobCarsPlaceSession
{
    private static bool _active;
    private static string? _jobId;
    private static int _expectedCars;
    private static bool _forceRegularDirection = true;
    private static string? _targetTrackId;
    private static float _aimX;
    private static float _aimY;
    private static float _aimZ;
    private static bool _hasAim;

    public static bool IsActive => _active;

    public static string? JobId => _jobId;

    public static int ExpectedCars => _expectedCars;

    public static bool ForceRegularDirection => _forceRegularDirection;

    public static string? TargetTrackId => _targetTrackId;

    public static bool HasAimPoint => _hasAim;

    public static void Begin(string jobId, int expectedCars)
    {
        var id = jobId?.Trim();
        if (string.IsNullOrEmpty(id) || expectedCars <= 0)
        {
            Clear();
            return;
        }

        _active = true;
        _jobId = id;
        _expectedCars = expectedCars;
        _forceRegularDirection = true;
        _targetTrackId = null;
        _hasAim = false;
    }

    public static void SetTargetTrack(string? trackId)
    {
        var t = trackId?.Trim();
        _targetTrackId = string.IsNullOrEmpty(t) ? null : t;
    }

    public static void SetAimPoint(float x, float y, float z)
    {
        _aimX = x;
        _aimY = y;
        _aimZ = z;
        _hasAim = true;
    }

    public static bool TryGetAimPoint(out float x, out float y, out float z)
    {
        x = _aimX;
        y = _aimY;
        z = _aimZ;
        return _hasAim;
    }

    public static void ClearAim()
    {
        _hasAim = false;
        _targetTrackId = null;
    }

    public static void ToggleFacing() => _forceRegularDirection = !_forceRegularDirection;

    public static void Clear()
    {
        _active = false;
        _jobId = null;
        _expectedCars = 0;
        _forceRegularDirection = true;
        _targetTrackId = null;
        _hasAim = false;
    }
}

/// <summary>Player Station Snap &amp; Return (3.1 paperwork).</summary>
public static class StationSnapSession
{
    private static bool _hasReturn;
    private static float _returnX;
    private static float _returnY;
    private static float _returnZ;

    public static bool HasReturnPoint => _hasReturn;

    public static void CaptureReturn(float x, float y, float z)
    {
        _returnX = x;
        _returnY = y;
        _returnZ = z;
        _hasReturn = true;
    }

    public static bool TryGetReturn(out float x, out float y, out float z)
    {
        x = _returnX;
        y = _returnY;
        z = _returnZ;
        return _hasReturn;
    }

    public static void Clear()
    {
        _hasReturn = false;
        _returnX = _returnY = _returnZ = 0f;
    }
}
