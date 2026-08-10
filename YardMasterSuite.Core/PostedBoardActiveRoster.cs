using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Parsed posted speed board — string-free for the fast HUD path (Active Roster).
/// Facing axes + dual km/h are captured on rare FoT refresh so tip Limit can keep
/// route/facing authority without re-reading <c>SignDebug.text</c> every tick.
/// </summary>
public readonly struct ParsedPostedBoard
{
    public ParsedPostedBoard(
        int instanceId,
        float x,
        float y,
        float z,
        float forwardX,
        float forwardZ,
        float rightX,
        float rightZ,
        float throughKmh,
        float divergeKmh,
        bool isDual,
        bool junctionNearby,
        string label)
    {
        InstanceId = instanceId;
        X = x;
        Y = y;
        Z = z;
        ForwardX = forwardX;
        ForwardZ = forwardZ;
        RightX = rightX;
        RightZ = rightZ;
        ThroughKmh = throughKmh;
        DivergeKmh = divergeKmh;
        IsDual = isDual;
        JunctionNearby = junctionNearby;
        Label = label ?? string.Empty;
    }

    public int InstanceId { get; }
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float ForwardX { get; }
    public float ForwardZ { get; }
    public float RightX { get; }
    public float RightZ { get; }
    public float ThroughKmh { get; }
    public float DivergeKmh { get; }
    public bool IsDual { get; }
    public bool JunctionNearby { get; }
    public string Label { get; }
}

/// <summary>
/// Spatial filter + governing-board helpers for posted Limit.
/// Heavy FoT/parse stays on a rare refresh; HUD ticks walk this roster with float math.
/// </summary>
public static class PostedBoardActiveRoster
{
    /// <summary>
    /// Keep signs within this radius of the refresh origin (covers tip lookback + ~1.6 km lookahead).
    /// </summary>
    public const float ActiveRadiusMeters = 2500f;

    /// <summary>
    /// Unused as a periodic timer — warm rosters do not re-FoT on a clock (that reintroduced hitch).
    /// Kept for docs / tests naming continuity with the 0.4.20.2 port.
    /// </summary>
    public const float RefreshSeconds = 45f;

    /// <summary>When FoT returned empty (streaming), retry after this delay — not every HUD tick.</summary>
    public const float EmptyRetrySeconds = 8f;

    /// <summary>Cap empty retries so a yard with no boards does not FoT forever.</summary>
    public const int MaxEmptyRetries = 3;

    /// <summary>Re-FoT when the loco has moved this far from the last roster origin.</summary>
    public const float MoveInvalidateMeters = 1000f;

    public static bool WithinActiveRadius(
        float signX,
        float signY,
        float signZ,
        float originX,
        float originY,
        float originZ)
    {
        var dx = signX - originX;
        var dy = signY - originY;
        var dz = signZ - originZ;
        var r = ActiveRadiusMeters;
        return (dx * dx) + (dy * dy) + (dz * dz) <= r * r;
    }

    /// <summary>
    /// Cab FoT policy: first sample, move invalidate, or a few empty retries for streaming.
    /// Never periodic FoT while the roster is warm.
    /// </summary>
    public static bool NeedsRefresh(
        float now,
        float lastRefreshAt,
        float originX,
        float originZ,
        float lastOriginX,
        float lastOriginZ,
        bool hasLastOrigin,
        bool rosterEmpty = false,
        int emptyRetriesDone = 0)
    {
        if (!hasLastOrigin || lastRefreshAt < 0f)
        {
            return true;
        }

        var dx = originX - lastOriginX;
        var dz = originZ - lastOriginZ;
        var m = MoveInvalidateMeters;
        if ((dx * dx) + (dz * dz) >= m * m)
        {
            return true;
        }

        if (rosterEmpty
            && emptyRetriesDone < MaxEmptyRetries
            && now - lastRefreshAt >= EmptyRetrySeconds)
        {
            return true;
        }

        return false;
    }

    public static float PickKmh(ParsedPostedBoard board, bool diverging) =>
        board.IsDual && diverging ? board.DivergeKmh : board.ThroughKmh;

    /// <summary>
    /// Closest board behind the loco along travel forward, within lookback (m).
    /// Simple corridor pick (0.4.20.2); tip authority still uses facing + path in TelemetryReader.
    /// </summary>
    public static float? SelectGoverningBehindKmh(
        ParsedPostedBoard[] boards,
        float locoX,
        float locoY,
        float locoZ,
        float forwardX,
        float forwardY,
        float forwardZ,
        float lookbackMeters)
    {
        if (boards == null || boards.Length == 0 || lookbackMeters <= 0f)
        {
            return null;
        }

        float? bestLimit = null;
        var bestAlong = float.NegativeInfinity;
        var lookbackSq = lookbackMeters * lookbackMeters;

        for (var i = 0; i < boards.Length; i++)
        {
            var board = boards[i];
            var dx = board.X - locoX;
            var dy = board.Y - locoY;
            var dz = board.Z - locoZ;
            var distSq = (dx * dx) + (dy * dy) + (dz * dz);
            if (distSq > lookbackSq)
            {
                continue;
            }

            var along = (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
            if (along >= 0f || along < -lookbackMeters)
            {
                continue;
            }

            if (along <= bestAlong)
            {
                continue;
            }

            bestAlong = along;
            bestLimit = board.ThroughKmh;
        }

        return bestLimit;
    }
}
