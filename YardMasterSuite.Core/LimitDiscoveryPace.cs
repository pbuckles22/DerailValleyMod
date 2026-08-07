namespace YardMasterSuite.Core;

/// <summary>
/// Cab-entry Limit discovery: never dump / finish the full SignDebug FoT list.
/// Find 1–2 boards per burst, pause ~100 ms, stop once a small cushion is warm
/// (wall-clock may reach ~1.1 s spread out — that is fine).
/// </summary>
public static class LimitDiscoveryPace
{
    /// <summary>Max signs to fully evaluate per burst (1 = less jerky if still cold).</summary>
    public const int SignsPerBurst = 1;

    /// <summary>Minimum seconds between bursts (~100 ms).</summary>
    public const float BurstIntervalSeconds = 0.1f;

    /// <summary>Hard cap on evaluations this warm — never walk the whole FoT dump.</summary>
    public const int MaxEvaluatePerWarm = 8;

    /// <summary>On-route ahead cushion (HUD Current + Next; dual junction ≤ this).</summary>
    public const int MaxAheadCushion = 4;

    /// <summary>True when another burst may run (interval elapsed).</summary>
    public static bool AllowBurst(float secondsSinceLastBurst) =>
        secondsSinceLastBurst >= BurstIntervalSeconds;

    /// <summary>True while this burst may evaluate another sign.</summary>
    public static bool ContinueBurst(int signsEvaluatedThisBurst) =>
        signsEvaluatedThisBurst < SignsPerBurst;

    /// <summary>
    /// Cab-entry smoke: discovery is done — do not keep walking remaining FoT signs.
    /// </summary>
    public static bool IsWarmEnough(
        bool hasBehindCurrent,
        int aheadGoverningCount,
        int evaluatedThisWarm)
    {
        if (evaluatedThisWarm >= MaxEvaluatePerWarm)
        {
            return true;
        }

        if (aheadGoverningCount >= MaxAheadCushion)
        {
            return true;
        }

        // One behind + at least one ahead is enough to paint Limit + Next and stop.
        if (hasBehindCurrent && aheadGoverningCount >= 1)
        {
            return true;
        }

        return false;
    }
}
