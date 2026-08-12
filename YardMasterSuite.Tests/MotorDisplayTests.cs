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
    public void StatusFromSignals_hot_when_temperature_at_or_over_threshold()
    {
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 110f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 105f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
    }

    [Theory]
    [InlineData(MotorCabTempBand.Warning)]
    [InlineData(MotorCabTempBand.Critical)]
    [InlineData(MotorCabTempBand.WarningAndCritical)]
    public void StatusFromSignals_hot_when_cab_mu_warning_or_critical(MotorCabTempBand cab)
    {
        // Cab TM TEMP yellow/red uses MU overheatStandardThreshold — below TM critical.
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 90f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f,
                cabTempBand: cab));
    }

    [Fact]
    public void StatusFromSignals_ok_when_cab_nominal_and_below_critical()
    {
        Assert.Equal(
            MotorStatus.Ok,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 90f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f,
                cabTempBand: MotorCabTempBand.Nominal));
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
    public void StatusFromSignals_dead_wins_over_cab_warning()
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsFuseOff,
                temperature: 90f,
                overheatingThreshold: 105f,
                workingMotors: 0f,
                totalMotors: 2f,
                cabTempBand: MotorCabTempBand.Warning));
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
    public void StatusFromSignals_dead_when_tm_knife_off_even_if_tms_still_ok()
    {
        // Idle: TMS often stays OK until throttle demanded — knife must still show Dead.
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f,
                cabTempBand: MotorCabTempBand.Nominal,
                tmFuseOn: false));
    }

    [Fact]
    public void StatusFromSignals_ok_from_temperature_when_knife_unknown()
    {
        // No knife read yet — keep prior cool-temp → OK (knife-off is the explicit Dead path).
        Assert.Equal(
            MotorStatus.Ok,
            MotorDisplay.StatusFromSignals(
                tmsState: null,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: null,
                totalMotors: null,
                cabTempBand: null,
                tmFuseOn: null));
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

    [Fact]
    public void FormatHud_forced_heat_percent_and_governor_flash()
    {
        Assert.Equal(
            $"<color={MotorDisplay.HotColor}>Motors Hot 50%</color>",
            MotorDisplay.FormatHud(MotorStatus.Hot, governorActive: false, flashOn: false, forcedHeatPercent: 50f));
        Assert.Equal(
            $"<color={MotorDisplay.HotColor}>Motors Hot</color>",
            MotorDisplay.FormatHud(MotorStatus.Hot, governorActive: true, flashOn: false));
        Assert.Equal(
            "<color=#FF6600>Motors Hot ▼GOV</color>",
            MotorDisplay.FormatHud(MotorStatus.Hot, governorActive: true, flashOn: true));
        Assert.Equal(
            "<color=#FF6600>Motors Hot 50% ▼GOV</color>",
            MotorDisplay.FormatHud(MotorStatus.Ok, governorActive: true, flashOn: true, forcedHeatPercent: 50f));
    }
}
