namespace YardMasterSuite.Core;

/// <summary>HUD chip for path check (3.4). Omit when no destination.</summary>
public static class PathCheckDisplay
{
    public static string? Format(PathCheckResult? result)
    {
        if (result == null || result.Status == PathCheckStatus.NoDestination)
        {
            return null;
        }

        return result.Status switch
        {
            PathCheckStatus.Aligned => "Path OK",
            PathCheckStatus.Misaligned => $"Path {result.MisalignedCount} switch",
            PathCheckStatus.NoPath => "Path none",
            PathCheckStatus.NoOrigin => "Path —",
            _ => null,
        };
    }
}
