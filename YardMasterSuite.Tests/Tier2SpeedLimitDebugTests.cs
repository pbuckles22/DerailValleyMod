using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2SpeedLimitDebugTests
{
    private static SpeedLimitDebugSnapshot NullTrain() =>
        new(hasLoco: false, speed: "— Speed", limit: "— Limit");

    private static SpeedLimitDebugSnapshot Live(
        string speed = "Speed 36 km/h",
        string limit = "Limit 60",
        string? drive = null,
        string? advice = null) =>
        new(hasLoco: true, speed, limit, detail: null, drive, advice);

    [Fact]
    public void NextLogMessage_logs_init_on_first_sample()
    {
        var msg = Tier2SpeedLimitDebug.NextLogMessage(null, NullTrain());
        Assert.Equal("T2 limit init (no-loco): — Speed  |  — Limit", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_gaining_loco()
    {
        var msg = Tier2SpeedLimitDebug.NextLogMessage(NullTrain(), Live());
        Assert.Equal("T2 limit loco: Speed 36 km/h  |  Limit 60", msg);
    }

    [Fact]
    public void NextLogMessage_logs_change_when_limit_changes()
    {
        var msg = Tier2SpeedLimitDebug.NextLogMessage(Live(limit: "Limit 60"), Live(limit: "Limit 40"));
        Assert.Equal("T2 limit change: Speed 36 km/h  |  Limit 40", msg);
    }

    [Fact]
    public void NextLogMessage_silent_when_only_speed_changes()
    {
        var prior = Live(speed: "Speed 36 km/h", limit: "Limit 60");
        var next = Live(speed: "Speed 40 km/h", limit: "Limit 60");
        Assert.Null(Tier2SpeedLimitDebug.NextLogMessage(prior, next));
    }

    [Fact]
    public void NextLogMessage_includes_detail_when_present()
    {
        var msg = Tier2SpeedLimitDebug.NextLogMessage(
            Live(limit: "Limit 60"),
            new SpeedLimitDebugSnapshot(true, "Speed 36 km/h", "Limit 30", "behind '3'=30 along=-12m"));
        Assert.Equal(
            "T2 limit change: Speed 36 km/h  |  Limit 30  |  behind '3'=30 along=-12m",
            msg);
    }

    [Fact]
    public void FormatDrive_shows_throttle_brake_pipe_grade_mass_and_type()
    {
        Assert.Equal(
            "Thr 40% Br 70% Ind 0% Pipe 5.2 grade=-1.2% mass=38t type=LocoDE2",
            LimitDriveDebug.FormatDrive(
                0.4f, 0.7f, 0f, 5.2f, -1.2f, 38f, "LocoDE2"));
    }

    [Fact]
    public void FormatAdvice_summarizes_level_and_target()
    {
        Assert.Equal("adv=None", LimitDriveDebug.FormatAdvice(BrakeAdvisoryState.Silent));
        var critical = new BrakeAdvisoryState(
            BrakeAdvisoryLevel.Critical, 30, 180, 12, "Brake 30 in 12 s (180 m)");
        Assert.Equal("adv=Critical 30 in 12s (180m)", LimitDriveDebug.FormatAdvice(critical));
    }

    [Fact]
    public void NextLogMessage_logs_when_brake_advisory_level_changes()
    {
        var prior = Live(
            limit: "Limit 70 (Posted)",
            drive: "Thr 50% Br 0% Ind 0% Pipe 6.0 grade=-1.0%",
            advice: "adv=None");
        var next = Live(
            speed: "Speed 80 km/h",
            limit: "Limit 70 (Posted)",
            drive: "Thr 50% Br 0% Ind 0% Pipe 6.0 grade=-1.0%",
            advice: "adv=Advisory 30 in 90s (1600m)");
        var msg = Tier2SpeedLimitDebug.NextLogMessage(prior, next);
        Assert.NotNull(msg);
        Assert.Contains("T2 limit change:", msg);
        Assert.Contains("adv=Advisory 30 in 90s (1600m)", msg!);
        Assert.Contains("Thr 50%", msg);
    }

    [Fact]
    public void NextLogMessage_logs_when_train_brake_enters_hard_bucket()
    {
        var prior = Live(
            limit: "Limit 30 (Recommended)",
            drive: "Thr 0% Br 20% Ind 0% Pipe 5.5 grade=-1.0%",
            advice: "adv=Critical 30 in 8s (120m)");
        var next = Live(
            speed: "Speed 55 km/h",
            limit: "Limit 30 (Recommended)",
            drive: "Thr 0% Br 80% Ind 0% Pipe 4.6 grade=-1.0%",
            advice: "adv=Critical 30 in 8s (120m)");
        var msg = Tier2SpeedLimitDebug.NextLogMessage(prior, next);
        Assert.NotNull(msg);
        Assert.Contains("Br 80%", msg!);
    }

    [Fact]
    public void NextLogMessage_silent_when_only_throttle_moves_inside_same_brake_bucket()
    {
        var prior = Live(
            limit: "Limit 60",
            drive: "Thr 40% Br 0% Ind 0% Pipe 6.0 grade=0.0%",
            advice: "adv=None");
        var next = Live(
            speed: "Speed 50 km/h",
            limit: "Limit 60",
            drive: "Thr 10% Br 0% Ind 0% Pipe 6.0 grade=0.0%",
            advice: "adv=None");
        Assert.Null(Tier2SpeedLimitDebug.NextLogMessage(prior, next));
    }

    [Theory]
    [InlineData(0, "idle")]
    [InlineData(20, "light")]
    [InlineData(50, "medium")]
    [InlineData(80, "hard")]
    public void BrakeBucketFromPercent_bands(int pct, string bucket)
    {
        Assert.Equal(bucket, SpeedLimitDebugSnapshot.BrakeBucketFromPercent(pct));
    }
}
