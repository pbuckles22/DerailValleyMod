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
