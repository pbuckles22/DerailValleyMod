using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab-entry aftershock: TraceBoardPass Main.Log storm while facing wobbles at standstill.
/// 0.6.50: board traces also wait for the Tier 2 telemetry-log opt-in (look-around choppiness).
/// </summary>
public class BoardTraceLogGateTests
{
    [Fact]
    public void Smoke_036_standstill_suppresses_board_trace_log()
    {
        Assert.False(BoardTraceLogGate.ShouldEmit(
            speedKmh: 0f,
            standstillMaxKmh: LimitDisplayHold.StandstillMaxSpeedKmh,
            telemetryLogsEnabled: true));
    }

    [Fact]
    public void Moving_allows_board_trace_log()
    {
        Assert.True(BoardTraceLogGate.ShouldEmit(
            speedKmh: 12f,
            standstillMaxKmh: LimitDisplayHold.StandstillMaxSpeedKmh,
            telemetryLogsEnabled: true));
    }

    [Fact]
    public void Driving_in_normal_play_writes_no_board_traces()
    {
        Assert.False(BoardTraceLogGate.ShouldEmit(
            speedKmh: 40f,
            standstillMaxKmh: LimitDisplayHold.StandstillMaxSpeedKmh,
            telemetryLogsEnabled: false));
    }
}
