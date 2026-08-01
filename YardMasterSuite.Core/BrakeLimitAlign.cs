namespace YardMasterSuite.Core;

/// <summary>
/// Keeps the Limit chip honest with the Brake chip (**1.16**).
/// <para>
/// 0.5.65 smoke: Brake fired <c>Runaway/Advisory 30</c> for a board ~3 km out while Limit stayed
/// <c>Posted 40/60</c> — aggressiveness had shortened Limit's adopt lead but Brake's planning
/// window still saw the board. Player rule: if Brake says N, Limit must show Recommended N.
/// </para>
/// </summary>
public static class BrakeLimitAlign
{
    /// <summary>
    /// When Brake has an active target tighter than the current Limit recommendation, adopt it
    /// for the Limit chip. Returns true when the recommendation was tightened.
    /// </summary>
    public static bool TryApply(
        float? recommendedKmh,
        float? recommendedAlongMeters,
        AheadBoard? brakeTarget,
        out float? alignedKmh,
        out float? alignedAlongMeters)
    {
        alignedKmh = recommendedKmh;
        alignedAlongMeters = recommendedAlongMeters;

        if (brakeTarget is not AheadBoard brake)
        {
            return false;
        }

        if (recommendedKmh is float rec && brake.Kmh + 0.5f >= rec)
        {
            return false;
        }

        alignedKmh = brake.Kmh;
        alignedAlongMeters = brake.AlongMeters;
        return true;
    }
}
