namespace YardMasterSuite.Core;

/// <summary>
/// Session-only path destination track id. Prefer <see cref="RouteDestSession"/> for 3.5.
/// Kept so End-pin and older call sites stay valid.
/// </summary>
public static class PathCheckSession
{
    public static bool HasDestination => RouteDestSession.HasDestination;

    public static string? DestinationTrackId => RouteDestSession.TrackId;

    public static void SetDestination(string? trackId) => RouteDestSession.SetTrackOnly(trackId);

    public static void Clear() => RouteDestSession.Clear();
}
