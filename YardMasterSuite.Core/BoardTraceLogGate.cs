namespace YardMasterSuite.Core;

/// <summary>
/// Gate for <c>T2 limit skip/take</c> Player.log lines (TraceBoardPass).
/// Standstill facing wobble flips board side and re-logs — suppress while stopped.
/// </summary>
public static class BoardTraceLogGate
{
    /// <summary>True when board take/skip traces may emit (player is moving).</summary>
    public static bool ShouldEmit(float speedKmh, float standstillMaxKmh) =>
        speedKmh > standstillMaxKmh;
}
