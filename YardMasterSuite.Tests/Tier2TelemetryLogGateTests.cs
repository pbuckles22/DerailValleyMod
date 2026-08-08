using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 0.6.49 smoke FAIL: sweeping the camera in a yard changed look-at / heading every HUD tick, so
/// change-driven <c>T2 …</c> lines wrote to Player.log ~10× per second and play felt choppy.
/// </summary>
public class Tier2TelemetryLogGateTests
{
    public Tier2TelemetryLogGateTests()
    {
        Tier2TelemetryLogGate.SetEnabled(false);
    }

    [Fact]
    public void Look_around_sweep_logs_nothing_in_normal_play()
    {
        Assert.False(Tier2TelemetryLogGate.Enabled);
        Assert.Equal(
            Tier2LogAction.Skip,
            Tier2TelemetryLogGate.Decide(Tier2TelemetryLogGate.Enabled, emittedLastTick: false));
    }

    [Fact]
    public void Skips_even_when_previous_tick_was_emitting()
    {
        Assert.Equal(
            Tier2LogAction.Skip,
            Tier2TelemetryLogGate.Decide(enabled: false, emittedLastTick: true));
    }

    [Fact]
    public void Smoke_enable_reinits_baselines_so_every_channel_logs_again()
    {
        Assert.True(Tier2TelemetryLogGate.Toggle());
        Assert.Equal(
            Tier2LogAction.ResetThenEmit,
            Tier2TelemetryLogGate.Decide(Tier2TelemetryLogGate.Enabled, emittedLastTick: false));
    }

    [Fact]
    public void Steady_smoke_session_logs_on_change_only()
    {
        Assert.Equal(
            Tier2LogAction.Emit,
            Tier2TelemetryLogGate.Decide(enabled: true, emittedLastTick: true));
    }

    [Fact]
    public void Toggle_returns_to_quiet_play()
    {
        Assert.True(Tier2TelemetryLogGate.Toggle());
        Assert.False(Tier2TelemetryLogGate.Toggle());
        Assert.False(Tier2TelemetryLogGate.Enabled);
    }
}
