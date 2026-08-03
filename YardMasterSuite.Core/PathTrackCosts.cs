using System;



namespace YardMasterSuite.Core;



/// <summary>Track class for through-lane / holding bias (3.5).</summary>

public enum PathTrackClass

{

    Unknown = 0,

    /// <summary>Main line / passenger / yard in-out / blow-through.</summary>

    Through = 1,

    /// <summary>Generic yard service (milder speed; still OK for transit).</summary>

    YardService = 2,

    /// <summary>Storage / loading / parking — avoid for through moves (cars may be there).</summary>

    SpurPocket = 3,

}



/// <summary>

/// Align Route edge costs in <b>seconds</b> (Google Maps / fastest ETA).

/// Base <c>TravelSeconds</c> = length/speed (+ junction). Spur / non-through / reverse

/// context is applied in <see cref="PathPlan"/> Dijkstra (pass-through architecture).

/// </summary>

public static class PathTrackCosts

{

    /// <summary>Legacy scalar costs (tests / PathCheck); Dijkstra uses <see cref="TravelSeconds"/>.</summary>

    public const float Through = 1f;

    public const float YardService = 3f;

    public const float SpurPocket = 8f;

    public const float Unknown = 5f;



    public const float DefaultThroughKmh = 70f;

    public const float DefaultYardServiceKmh = 40f;

    public const float DefaultSpurPocketKmh = 20f;

    public const float DefaultUnknownKmh = 50f;



    /// <summary>Stop + reverse (seconds).</summary>

    public const float ReversePenalty = 120f;



    /// <summary>Slow through a switch (seconds).</summary>

    public const float JunctionPenaltySeconds = 5f;



    /// <summary>

    /// Extra cost for entering storage/loading/parking in intermediate transit.

    /// Origin/dest yards use half (see <see cref="PathPlan"/>).

    /// </summary>

    public const float SpurOccupancyPenaltySeconds = 180f;



    /// <summary>

    /// Prefer clear Through lanes over YardService / Unknown when both are legal.

    /// </summary>

    public const float NonThroughPenaltySeconds = 45f;



    public const float MinLengthMeters = 1f;

    public const float MinSpeedKmh = 5f;



    public static float EnterCost(PathTrackClass trackClass) =>

        trackClass switch

        {

            PathTrackClass.Through => Through,

            PathTrackClass.YardService => YardService,

            PathTrackClass.SpurPocket => SpurPocket,

            PathTrackClass.Unknown => Unknown,

            _ => Unknown,

        };



    public static float DefaultSpeedKmh(PathTrackClass trackClass) =>

        trackClass switch

        {

            PathTrackClass.Through => DefaultThroughKmh,

            PathTrackClass.YardService => DefaultYardServiceKmh,

            PathTrackClass.SpurPocket => DefaultSpurPocketKmh,

            _ => DefaultUnknownKmh,

        };



    /// <summary>

    /// Effective planning speed (km/h): geometry when known, else class default.

    /// Yard/spur caps prevent treating holding tracks as mainline raceways.

    /// </summary>

    public static float PlanningSpeedKmh(float? geometryLimitKmh, PathTrackClass trackClass)

    {

        var fallback = DefaultSpeedKmh(trackClass);

        var speed = geometryLimitKmh is float g && g > 0f ? g : fallback;



        // Never plan faster than the class allows on non-through lanes.

        if (trackClass == PathTrackClass.SpurPocket)

        {

            speed = Math.Min(speed, DefaultSpurPocketKmh);

        }

        else if (trackClass == PathTrackClass.YardService)

        {

            speed = Math.Min(speed, DefaultYardServiceKmh);

        }



        return Math.Max(MinSpeedKmh, speed);

    }



    /// <summary>

    /// Base Dijkstra edge travel seconds (length/speed + optional junction).

    /// Spur / non-through penalties are applied in <see cref="PathPlan"/> with yard context.

    /// </summary>

    public static float TravelSeconds(

        float lengthMeters,

        float? geometryLimitKmh,

        PathTrackClass trackClass,

        bool junctionHop = false)

    {

        var len = lengthMeters > 0f ? lengthMeters : MinLengthMeters;

        var kmh = PlanningSpeedKmh(geometryLimitKmh, trackClass);

        var mps = SpeedDisplay.ToMetersPerSecond(kmh);

        if (mps < 0.01f)

        {

            mps = SpeedDisplay.ToMetersPerSecond(MinSpeedKmh);

        }



        var seconds = len / mps;

        if (junctionHop)

        {

            seconds += JunctionPenaltySeconds;

        }



        return seconds;

    }



    /// <summary>Backward-compatible name — time-cost hop (seconds).</summary>

    public static float HopCost(float lengthMeters, PathTrackClass trackClass) =>

        TravelSeconds(lengthMeters, geometryLimitKmh: null, trackClass);



    /// <summary>Map DV TrackID type tokens (STORAGE, LOADING, …) to a class.</summary>

    public static PathTrackClass Classify(string? typeToken)

    {

        var t = typeToken?.Trim();

        if (string.IsNullOrEmpty(t))

        {

            return PathTrackClass.Unknown;

        }



        t = t!.ToUpperInvariant();

        // Through / pass-through first (LOADING_PASSENGER hits PASSENGER before LOADING).

        if (t.Contains("MAIN") || t.Contains("PASSENGER") || t == "I" || t == "O"

            || t.Contains("REGULAR_IN") || t.Contains("REGULAR_OUT")

            || t.Contains("BLOW") || t.Contains("THROUGH") || t.Contains("PASS_")
            || LooksLikeNamedInOutTrack(t))

        {

            return PathTrackClass.Through;

        }



        if (t.Contains("STORAGE") || t.Contains("LOADING") || t.Contains("PARKING"))

        {

            return PathTrackClass.SpurPocket;

        }



        return PathTrackClass.YardService;

    }

    /// <summary>DV display ids such as HB-G3O / HB-C2I when trackType reflection is absent.</summary>
    private static bool LooksLikeNamedInOutTrack(string token)
    {
        if (token.Length < 4 || token.IndexOf('-') <= 0)
        {
            return false;
        }

        var suffix = token[token.Length - 1];
        return (suffix == 'I' || suffix == 'O')
            && char.IsDigit(token[token.Length - 2]);
    }

}


