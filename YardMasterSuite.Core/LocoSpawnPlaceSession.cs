namespace YardMasterSuite.Core;

/// <summary>Active place-mode session for license-gated loco spawn (3.1b).</summary>
public static class LocoSpawnPlaceSession
{
    private static bool _active;
    private static int _selectedIndex;
    private static bool _forceRegularDirection = true;
    private static string? _targetTrackId;
    private static float _aimX;
    private static float _aimY;
    private static float _aimZ;
    private static bool _hasAim;
    private static bool _placeOk;

    public static bool IsActive => _active;

    public static int SelectedIndex => _selectedIndex;

    public static bool ForceRegularDirection => _forceRegularDirection;

    public static string? TargetTrackId => _targetTrackId;

    public static bool HasAimPoint => _hasAim;

    /// <summary>Last frame place OK (blue ghost) vs blocked (red).</summary>
    public static bool PlaceOk => _placeOk;

    public static void Begin(int selectedIndex = 0)
    {
        _active = true;
        _selectedIndex = selectedIndex < 0 ? 0 : selectedIndex;
        _forceRegularDirection = true;
        _targetTrackId = null;
        _hasAim = false;
        _placeOk = false;
    }

    public static void SetSelectedIndex(int index) => _selectedIndex = index < 0 ? 0 : index;

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
        _placeOk = false;
    }

    public static void SetPlaceOk(bool ok) => _placeOk = ok;

    public static void ToggleFacing() => _forceRegularDirection = !_forceRegularDirection;

    public static void Clear()
    {
        _active = false;
        _selectedIndex = 0;
        _forceRegularDirection = true;
        _targetTrackId = null;
        _hasAim = false;
        _placeOk = false;
    }
}
