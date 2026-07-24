namespace YardMasterSuite.Core;

/// <summary>
/// Session-only park mark (1.14). Not persisted across game restarts.
/// Stores XZ plus Y at mark time so the AR pin stays at ground/stand height.
/// </summary>
public static class ParkMarkSession
{
    private static bool _hasMark;
    private static float _x;
    private static float _y;
    private static float _z;

    public static bool HasMark => _hasMark;

    public static bool TryGet(out float x, out float z)
    {
        x = _x;
        z = _z;
        return _hasMark;
    }

    public static bool TryGet(out float x, out float y, out float z)
    {
        x = _x;
        y = _y;
        z = _z;
        return _hasMark;
    }

    public static void Set(float x, float z) => Set(x, 0f, z);

    public static void Set(float x, float y, float z)
    {
        _x = x;
        _y = y;
        _z = z;
        _hasMark = true;
    }

    public static void Clear()
    {
        _hasMark = false;
        _x = 0f;
        _y = 0f;
        _z = 0f;
    }
}
