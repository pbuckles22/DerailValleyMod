namespace YardMasterSuite.Core;

/// <summary>
/// Gate for <c>T2 limit skip/take</c> Player.log lines (TraceBoardPass).
/// Standstill facing wobble flips board side and re-logs — suppress while stopped.
/// Each emitted line also re-parses the board, so this gate saves scan work too.
/// </summary>
public static class BoardTraceLogGate
{
    /// <summary>True when board take/skip traces may emit (Tier 2 logs on, and player is moving).</summary>
    public static bool ShouldEmit(float speedKmh, float standstillMaxKmh, bool telemetryLogsEnabled) =>
        telemetryLogsEnabled && speedKmh > standstillMaxKmh;
}
