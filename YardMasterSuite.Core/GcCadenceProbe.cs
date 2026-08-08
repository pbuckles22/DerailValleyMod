namespace YardMasterSuite.Core;

/// <summary>
/// Throttle for the <c>T2 perf gc</c> probe. The rhythmic hitch is a stop-the-world collection, so
/// the useful signal is "how many gen-0 collections happened per window", not a per-frame number.
/// Ungated (not behind the Tier 2 log opt-in) because it must be visible in a normal play smoke.
/// </summary>
public static class GcCadenceProbe
{
    public const float DefaultWindowSeconds = 5f;

    public static bool ShouldLog(
        float secondsSinceLastLog,
        int gen0CollectionsInWindow,
        float windowSeconds = DefaultWindowSeconds) =>
        gen0CollectionsInWindow > 0 && secondsSinceLastLog >= windowSeconds;
}
