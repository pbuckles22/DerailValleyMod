namespace YardMasterSuite.Core;

/// <summary>
/// Office hide AABB: resolve FoT once per town (yard id), not on a timer.
/// </summary>
public static class OfficeBoundsCachePolicy
{
    /// <summary>
    /// True when we must re-resolve (FindObjectsOfType / mesh walk).
    /// Same yard with an existing cache → reuse.
    /// </summary>
    public static bool ShouldResolve(string? cachedYardId, string? currentYardId, bool hasCache)
    {
        if (string.IsNullOrWhiteSpace(currentYardId))
        {
            return false;
        }

        if (!hasCache)
        {
            return true;
        }

        var cached = cachedYardId?.Trim() ?? "";
        var current = currentYardId!.Trim();
        return !string.Equals(cached, current, System.StringComparison.OrdinalIgnoreCase);
    }
}
