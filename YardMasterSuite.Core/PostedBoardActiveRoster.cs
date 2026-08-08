using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Parsed posted speed board — string-free for the fast HUD path (Active Roster).
/// </summary>
public readonly struct ParsedPostedBoard
{
    public ParsedPostedBoard(float x, float y, float z, float kmh)
    {
        X = x;
        Y = y;
        Z = z;
        Kmh = kmh;
    }

    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float Kmh { get; }
}

/// <summary>
/// Spatial filter + governing-board pick for posted Limit (0.4.20.2).
/// Heavy FoT/parse stays on a slow refresh; 10 Hz only walks this roster with float math.
/// </summary>
public static class PostedBoardActiveRoster
{
    /// <summary>Keep signs within this radius of the refresh origin (loco / player).</summary>
    public const float ActiveRadiusMeters = 600f;

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
    /// Closest board behind the loco along travel forward, within lookback (m). Same polarity as 0.4.20.
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
            // Behind in travel direction (just passed).
            if (along >= 0f || along < -lookbackMeters)
            {
                continue;
            }

            if (along <= bestAlong)
            {
                continue;
            }

            bestAlong = along;
            bestLimit = board.Kmh;
        }

        return bestLimit;
    }
}
