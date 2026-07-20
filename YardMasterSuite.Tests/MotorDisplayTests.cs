using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MotorDisplayTests
{
    [Fact]
    public void StatusFromSignals_null_when_no_tm_signals()
    {
        Assert.Null(MotorDisplay.StatusFromSignals(null, null, null, null, null));
    }

    [Theory]
    [InlineData(MotorDisplay.TmsFuseOff)]
    [InlineData(MotorDisplay.TmsHasDead)]
    public void StatusFromSignals_dead_from_tms(float tms)
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(tms, temperature: 40f, overheatingThreshold: 105f, workingMotors: 2f, totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_dead_when_working_count_below_total()
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 1f,
                totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_hot_when_temperature_over_threshold()
    {
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 110f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_ok_when_cool_and_alive()
    {
        Assert.Equal(
            MotorStatus.Ok,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_dead_wins_over_hot()
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsFuseOff,
                temperature: 120f,
                overheatingThreshold: 105f,
                workingMotors: 0f,
                totalMotors: 2f));
    }

    [Fact]
    public void Format_shows_placeholder_and_plain_labels()
    {
        Assert.Equal("— Motors", MotorDisplay.Format(null));
        Assert.Equal("Motors OK", MotorDisplay.Format(MotorStatus.Ok));
        Assert.Equal("Motors Hot", MotorDisplay.Format(MotorStatus.Hot));
        Assert.Equal("Motors Dead", MotorDisplay.Format(MotorStatus.Dead));
    }

    [Fact]
    public void FormatHud_colors_ok_hot_dead()
    {
        Assert.Equal("— Motors", MotorDisplay.FormatHud(null));
        Assert.Equal(
            $"<color={MotorDisplay.OkColor}>Motors OK</color>",
            MotorDisplay.FormatHud(MotorStatus.Ok));
        Assert.Equal(
            $"<color={MotorDisplay.HotColor}>Motors Hot</color>",
            MotorDisplay.FormatHud(MotorStatus.Hot));
        Assert.Equal(
            $"<color={MotorDisplay.DeadColor}>Motors Dead</color>",
            MotorDisplay.FormatHud(MotorStatus.Dead));
    }
}
