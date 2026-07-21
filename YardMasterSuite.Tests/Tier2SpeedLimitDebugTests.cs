using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class Tier2SpeedLimitDebugTests
{
    private static SpeedLimitDebugSnapshot NullTrain() =>
        new(hasLoco: false, speed: "— Speed", limit: "— Limit");

    private static SpeedLimitDebugSnapshot Live(
        string speed = "Speed 36 km/h",
        string limit = "Limit 60") =>
        new(hasLoco: true, speed, limit);

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
    public void NextLogMessage_silent_when_unchanged()
    {
        var snap = Live();
        Assert.Null(Tier2SpeedLimitDebug.NextLogMessage(snap, snap));
    }
}
