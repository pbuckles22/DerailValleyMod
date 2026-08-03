using System;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class PathGraphBuildPumpTests
{
    [Fact]
    public void Begin_starts_mapping_at_zero_progress()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(200);

        Assert.True(pump.IsMapping);
        Assert.Equal(PathGraphBuildPump.State.Mapping, pump.Current);
        Assert.Equal(200, pump.TotalUnits);
        Assert.Equal(0, pump.CompletedUnits);
        Assert.Equal(0f, pump.Progress01);
        Assert.Equal(200, pump.RemainingUnits);
    }

    [Fact]
    public void AddCompleted_advances_progress_in_budget_chunks()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(100);

        pump.AddCompleted(40);
        Assert.Equal(0.4f, pump.Progress01, precision: 3);
        Assert.Equal(60, pump.RemainingUnits);

        pump.AddCompleted(60);
        Assert.Equal(1f, pump.Progress01, precision: 3);
        Assert.Equal(0, pump.RemainingUnits);
        Assert.True(pump.IsMapping);
    }

    [Fact]
    public void AddCompleted_does_not_exceed_total()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(10);
        pump.AddCompleted(50);

        Assert.Equal(10, pump.CompletedUnits);
        Assert.Equal(1f, pump.Progress01);
    }

    [Fact]
    public void Complete_marks_ready_and_full_progress()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(80);
        pump.AddCompleted(20);
        pump.Complete();

        Assert.Equal(PathGraphBuildPump.State.Ready, pump.Current);
        Assert.False(pump.IsMapping);
        Assert.Equal(1f, pump.Progress01);
    }

    [Fact]
    public void Fail_and_Reset_leave_idle_or_failed_not_mapping()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(50);
        pump.Fail();
        Assert.Equal(PathGraphBuildPump.State.Failed, pump.Current);
        Assert.False(pump.IsMapping);

        pump.Reset();
        Assert.Equal(PathGraphBuildPump.State.Idle, pump.Current);
        Assert.Equal(0, pump.TotalUnits);
        Assert.Equal(0f, pump.Progress01);
    }

    [Theory]
    [InlineData(0f, "Station mapping… 0%")]
    [InlineData(0.35f, "Station mapping… 35%")]
    [InlineData(1f, "Station mapping… 100%")]
    public void FormatBanner_shows_percent(float progress, string expected)
    {
        Assert.Equal(expected, PathGraphBuildPump.FormatBanner(progress));
    }

    [Fact]
    public void Simulated_frame_budget_never_processes_more_than_max_per_tick()
    {
        const int total = 250;
        const int maxPerTick = 64;
        var pump = new PathGraphBuildPump();
        pump.Begin(total);

        var ticks = 0;
        while (pump.RemainingUnits > 0)
        {
            var chunk = Math.Min(maxPerTick, pump.RemainingUnits);
            Assert.True(chunk <= maxPerTick);
            Assert.True(chunk > 0);
            pump.AddCompleted(chunk);
            ticks++;
        }

        Assert.True(ticks >= 4); // 250 / 64 → at least 4 frames
        pump.Complete();
        Assert.Equal(PathGraphBuildPump.State.Ready, pump.Current);
    }
}
