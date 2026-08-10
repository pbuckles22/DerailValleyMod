using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Posted Limit FILO — soft-loaded exit corridors (≤ MaxDepth boards each way).
/// Cab never discovers; only town / switch / Align / reverse events rebuild.
/// </summary>
public static class PostedLimitFilo
{
    /// <summary>Cap per exit corridor (Current + Next cushion).</summary>
    public const int MaxDepth = 5;

    /// <summary>Lock travel polarity once moving faster than this (drop opposite exit).</summary>
    public const float DirectionLockMinSpeedKmh = 5f;

    public static bool ShouldLockDirection(float speedKmh) =>
        speedKmh > DirectionLockMinSpeedKmh;

    /// <summary>
    /// Split parsed boards into +travel exit vs −travel exit, nearest-first, capped.
    /// </summary>
    public static void PartitionExits(
        ParsedPostedBoard[] boards,
        float originX,
        float originY,
        float originZ,
        float forwardX,
        float forwardY,
        float forwardZ,
        out ParsedPostedBoard[] exitPlus,
        out ParsedPostedBoard[] exitMinus)
    {
        exitPlus = Array.Empty<ParsedPostedBoard>();
        exitMinus = Array.Empty<ParsedPostedBoard>();
        if (boards == null || boards.Length == 0)
        {
            return;
        }

        var plus = new Ranked[boards.Length];
        var minus = new Ranked[boards.Length];
        var plusN = 0;
        var minusN = 0;

        for (var i = 0; i < boards.Length; i++)
        {
            var b = boards[i];
            var dx = b.X - originX;
            var dy = b.Y - originY;
            var dz = b.Z - originZ;
            var along = (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
            if (along > 0f)
            {
                plus[plusN++] = new Ranked(along, b);
            }
            else if (along < 0f)
            {
                minus[minusN++] = new Ranked(-along, b);
            }
        }

        exitPlus = TakeNearest(plus, plusN);
        exitMinus = TakeNearest(minus, minusN);
    }

    /// <summary>Pick the exit that matches current travel vs warm-time forward.</summary>
    public static ParsedPostedBoard[] SelectActiveExit(
        ParsedPostedBoard[] exitPlus,
        ParsedPostedBoard[] exitMinus,
        float warmForwardX,
        float warmForwardZ,
        float travelX,
        float travelZ)
    {
        var warmLen = Math.Sqrt((warmForwardX * warmForwardX) + (warmForwardZ * warmForwardZ));
        var travelLen = Math.Sqrt((travelX * travelX) + (travelZ * travelZ));
        if (warmLen < 1e-4 || travelLen < 1e-4)
        {
            return exitPlus ?? Array.Empty<ParsedPostedBoard>();
        }

        var dot = ((warmForwardX * travelX) + (warmForwardZ * travelZ)) / (warmLen * travelLen);
        return dot >= 0.0
            ? exitPlus ?? Array.Empty<ParsedPostedBoard>()
            : exitMinus ?? Array.Empty<ParsedPostedBoard>();
    }

    private static ParsedPostedBoard[] TakeNearest(Ranked[] ranked, int count)
    {
        if (count <= 0)
        {
            return Array.Empty<ParsedPostedBoard>();
        }

        Array.Sort(ranked, 0, count, RankedComparer.Instance);
        var n = count < MaxDepth ? count : MaxDepth;
        var result = new ParsedPostedBoard[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = ranked[i].Board;
        }

        return result;
    }

    private readonly struct Ranked
    {
        public Ranked(float distance, ParsedPostedBoard board)
        {
            Distance = distance;
            Board = board;
        }

        public float Distance { get; }
        public ParsedPostedBoard Board { get; }
    }

    private sealed class RankedComparer : System.Collections.Generic.IComparer<Ranked>
    {
        public static readonly RankedComparer Instance = new();

        public int Compare(Ranked x, Ranked y) => x.Distance.CompareTo(y.Distance);
    }
}
