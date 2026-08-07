namespace YardMasterSuite.Core;

/// <summary>
/// Align-gated board cache: only 4–8 on-route signs, sync before Align returns OK.
/// </summary>
public static class BoardCacheWarmPolicy
{
    /// <summary>Max boards to attach for a planned corridor (Current + Next cushion + junction dual).</summary>
    public const int MaxOnRouteSigns = 8;

    /// <summary>Target min on-route attaches when corridor is known.</summary>
    public const int MinOnRouteSigns = 4;

    /// <summary>
    /// Max ms Align may spend warming boards before returning OK to the player (~0.5 s).
    /// </summary>
    public const long AlignBudgetMilliseconds = 500;

    public static bool ContinueAlignAttach(int attachedOnRoute, long elapsedMilliseconds) =>
        attachedOnRoute < MaxOnRouteSigns
        && elapsedMilliseconds < AlignBudgetMilliseconds;

    /// <summary>True when we have enough on-route boards or hit the cap.</summary>
    public static bool AlignWarmComplete(int attachedOnRoute) =>
        attachedOnRoute >= MinOnRouteSigns;

    /// <summary>Stop sync warm early once complete or budget exhausted.</summary>
    public static bool ShouldStopAlignSync(int attachedOnRoute, long elapsedMilliseconds) =>
        attachedOnRoute >= MaxOnRouteSigns
        || elapsedMilliseconds >= AlignBudgetMilliseconds
        || (attachedOnRoute >= MinOnRouteSigns && elapsedMilliseconds >= AlignBudgetMilliseconds);
}
