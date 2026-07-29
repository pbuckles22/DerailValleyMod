using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Track class for through-lane cost bias (3.5).</summary>
public enum PathTrackClass
{
    Unknown = 0,
    /// <summary>Main line / passenger / yard in-out / blow-through.</summary>
    Through = 1,
    /// <summary>Generic yard service (mild penalty).</summary>
    YardService = 2,
    /// <summary>Storage / loading / parking — avoid for through moves.</summary>
    SpurPocket = 3,
}

/// <summary>Cost multipliers applied when entering a track of that class.</summary>
public static class PathTrackCosts
{
    public const float Through = 1f;
    public const float YardService = 3f;
    public const float SpurPocket = 8f;
    public const float Unknown = 2f;
    public const float ReversePenalty = 25f;

    public static float EnterCost(PathTrackClass trackClass) =>
        trackClass switch
        {
            PathTrackClass.Through => Through,
            PathTrackClass.YardService => YardService,
            PathTrackClass.SpurPocket => SpurPocket,
            _ => Unknown,
        };

    /// <summary>Map DV TrackID type tokens (STORAGE, LOADING, …) to a class.</summary>
    public static PathTrackClass Classify(string? typeToken)
    {
        var t = typeToken?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return PathTrackClass.Unknown;
        }

        t = t!.ToUpperInvariant();
        if (t.Contains("MAIN") || t.Contains("PASSENGER") || t == "I" || t == "O"
            || t.Contains("REGULAR_IN") || t.Contains("REGULAR_OUT"))
        {
            return PathTrackClass.Through;
        }

        if (t.Contains("STORAGE") || t.Contains("LOADING") || t.Contains("PARKING"))
        {
            return PathTrackClass.SpurPocket;
        }

        return PathTrackClass.YardService;
    }
}
