namespace YardMasterSuite.Core;

/// <summary>
/// Formats Player.log timing lines for HUD/Limit hitch diagnosis (0.6.39).
/// </summary>
public static class HudPerfLog
{
    public const string Prefix = "T2 perf";

    public static string FormatTrainBar(
        long totalMs,
        long fluidsMs,
        long limitMs,
        long massLeversMs,
        long restMs) =>
        $"{Prefix} train: total={totalMs}ms fluids={fluidsMs}ms limit={limitMs}ms "
        + $"massLevers={massLeversMs}ms rest={restMs}ms";

    /// <summary>Other-loco radar scene scan — the 1 Hz hitch suspect (0.6.51).</summary>
    public static string FormatRadarScan(long scanMs, int carsSeen, int kept) =>
        $"{Prefix} radar: scan={scanMs}ms cars={carsSeen} kept={kept}";

    /// <summary>
    /// Gen-0 cadence for the rhythmic hitch (0.6.54). Player.log line, one per probe window.
    /// </summary>
    public static string FormatGcCadence(int gen0InWindow, float windowSeconds, long heapBytes) =>
        $"{Prefix} gc: gen0={gen0InWindow}/{windowSeconds:0.#}s heap={heapBytes / (1024 * 1024)}MB";

    public static string FormatLimitScan(
        long totalMs,
        long signFoTMs,
        long pathBuildMs,
        long signWalkMs,
        int signCount,
        int pathSegs) =>
        $"{Prefix} limit: total={totalMs}ms fot={signFoTMs}ms path={pathBuildMs}ms "
        + $"walk={signWalkMs}ms signs={signCount} segs={pathSegs}";
}
