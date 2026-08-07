using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab-entry aftershock: TraceBoardPass Main.Log storm while facing wobbles at standstill.
/// </summary>
public class BoardTraceLogGateTests
{
    [Fact]
    public void Smoke_036_standstill_suppresses_board_trace_log()
    {
        Assert.False(BoardTraceLogGate.ShouldEmit(
            speedKmh: 0f,
            standstillMaxKmh: LimitDisplayHold.StandstillMaxSpeedKmh));
    }

    [Fact]
    public void Moving_allows_board_trace_log()
    {
        Assert.True(BoardTraceLogGate.ShouldEmit(
            speedKmh: 12f,
            standstillMaxKmh: LimitDisplayHold.StandstillMaxSpeedKmh));
    }
}
