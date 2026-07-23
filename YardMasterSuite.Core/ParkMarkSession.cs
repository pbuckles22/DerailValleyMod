namespace YardMasterSuite.Core;

/// <summary>
/// Session-only park mark (1.14). Not persisted across game restarts.
/// </summary>
public static class ParkMarkSession
{
    private static bool _hasMark;
    private static float _x;
    private static float _z;

    public static bool HasMark => _hasMark;

    public static bool TryGet(out float x, out float z)
    {
        x = _x;
        z = _z;
        return _hasMark;
    }

    public static void Set(float x, float z)
    {
        _x = x;
        _z = z;
        _hasMark = true;
    }

    public static void Clear()
    {
        _hasMark = false;
        _x = 0f;
        _z = 0f;
    }
}
