namespace YardMasterSuite.Core;

/// <summary>What a HUD tick may do with per-tick <c>T2 …</c> telemetry lines.</summary>
public enum Tier2LogAction
{
    /// <summary>Skip the snapshot + log work entirely.</summary>
    Skip,

    /// <summary>First tick after enable: drop stale baselines so every channel re-logs <c>init</c>.</summary>
    ResetThenEmit,

    /// <summary>Steady state: log on change only.</summary>
    Emit,
}

/// <summary>
/// Opt-in gate for change-driven <c>T2 …</c> telemetry lines (heading, look-at, coupler, board trace).
/// Sweeping the camera re-resolves look-at every HUD tick, so those channels wrote tens of
/// <c>Main.Log</c> lines per second and made look-around choppy (0.6.49 smoke FAIL). No player-facing
/// HUD chip reads them, so they stay off until Tier 2 asks for them (Shift+F2).
/// Event lines (hotkey feedback, path set/clear, perf) are not gated.
/// </summary>
public static class Tier2TelemetryLogGate
{
    /// <summary>False in normal play; Tier 2 smoke turns it on.</summary>
    public static bool Enabled { get; private set; }

    public static bool Toggle()
    {
        Enabled = !Enabled;
        return Enabled;
    }

    public static void SetEnabled(bool enabled) => Enabled = enabled;

    public static Tier2LogAction Decide(bool enabled, bool emittedLastTick)
    {
        if (!enabled)
        {
            return Tier2LogAction.Skip;
        }

        return emittedLastTick ? Tier2LogAction.Emit : Tier2LogAction.ResetThenEmit;
    }
}
