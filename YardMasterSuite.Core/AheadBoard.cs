using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One governing board ahead of the loco (km/h + along-track meters).</summary>
public readonly struct AheadBoard
{
    public AheadBoard(float kmh, float alongMeters)
    {
        Kmh = kmh;
        AlongMeters = alongMeters;
    }

    public float Kmh { get; }
    public float AlongMeters { get; }
}

/// <summary>Look-ahead helpers for Posted Limit + Next (**1.17**).</summary>
public static class AheadBoards
{
    /// <summary>Nearest board ahead whose number differs from <paramref name="fromKmh"/>.</summary>
    public static AheadBoard? NextDifferent(float? fromKmh, IReadOnlyList<AheadBoard>? aheadBoards)
    {
        if (fromKmh is not float from || aheadBoards == null)
        {
            return null;
        }

        var fromWhole = (int)Math.Round(from, MidpointRounding.AwayFromZero);
        AheadBoard? best = null;
        for (var i = 0; i < aheadBoards.Count; i++)
        {
            var board = aheadBoards[i];
            if (board.AlongMeters <= 0f)
            {
                continue;
            }

            var whole = (int)Math.Round(board.Kmh, MidpointRounding.AwayFromZero);
            if (whole == fromWhole)
            {
                continue;
            }

            if (best is null || board.AlongMeters < best.Value.AlongMeters)
            {
                best = board;
            }
        }

        return best;
    }
}
