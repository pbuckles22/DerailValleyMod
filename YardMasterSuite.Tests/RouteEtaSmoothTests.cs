using System;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class RouteEtaSmoothTests
{
    [Fact]
    public void First_tick_follows_plan_remaining()
    {
        var lag = 0f;
        var arrival = -1f;
        var prevPlan = -1f;
        var displayed = -1f;
        var eta = RouteEtaSmooth.Tick(1517f, null, 100f, ref lag, ref arrival, ref prevPlan, ref displayed);

        Assert.InRange(eta, 1516.5f, 1517.5f);
        Assert.Equal(0f, lag);
        Assert.InRange(arrival, 1616.5f, 1617.5f);
        Assert.InRange(displayed, 1516.5f, 1517.5f);
    }

    [Fact]
    public void Arrival_clamp_blocks_multi_minute_pace_jumps_from_trip_samples()
    {
        var lag = 0f;
        var arrival = -1f;
        var prevPlan = -1f;
        var displayed = -1f;
        var now = 0f;
        var planRem = 25f * 60f + 17f;
        // Seed while "moving" so later pace spikes exercise the clamp path.
        var eta = RouteEtaSmooth.Tick(planRem, planRem, now, ref lag, ref arrival, ref prevPlan, ref displayed);

        now += 1f;
        var spike = 31f * 60f + 33f;
        var afterSpike = RouteEtaSmooth.Tick(planRem, spike, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        Assert.True(Math.Abs(afterSpike - eta) <= RouteEtaSmooth.MaxRemainingJumpPerSecond());

        now += 1f;
        var plunge = 17f * 60f + 6f;
        var afterPlunge = RouteEtaSmooth.Tick(planRem, plunge, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        Assert.True(Math.Abs(afterPlunge - afterSpike) <= RouteEtaSmooth.MaxRemainingJumpPerSecond());
    }

    [Fact]
    public void One_hertz_accel_brake_series_stays_within_clamp()
    {
        var lag = 0f;
        var arrival = -1f;
        var prevPlan = -1f;
        var displayed = -1f;
        var now = 1000f;
        var planRem = 9f * 60f + 53f;
        var prev = RouteEtaSmooth.Tick(planRem, planRem, now, ref lag, ref arrival, ref prevPlan, ref displayed);

        float[] paceHints =
        {
            15f * 60f + 45f,
            12f * 60f,
            8f * 60f,
            10f * 60f,
            planRem,
        };

        foreach (var hint in paceHints)
        {
            now += 1f;
            planRem = Math.Max(0f, planRem - 3f);
            var next = RouteEtaSmooth.Tick(planRem, hint, now, ref lag, ref arrival, ref prevPlan, ref displayed);
            Assert.True(
                Math.Abs(next - prev) <= RouteEtaSmooth.MaxRemainingJumpPerSecond() + 0.01f,
                $"jump {prev} → {next} at hint={hint}");
            prev = next;
        }
    }

    [Fact]
    public void Standing_still_freezes_eta_no_wall_clock_countdown()
    {
        var lag = 0f;
        var arrival = -1f;
        var prevPlan = -1f;
        var displayed = -1f;
        var now = 50f;
        var planRem = 600f;
        var a = RouteEtaSmooth.Tick(planRem, null, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        now += 1f;
        var b = RouteEtaSmooth.Tick(planRem, null, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        now += 30f;
        var c = RouteEtaSmooth.Tick(planRem, null, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        Assert.Equal(a, b);
        Assert.Equal(a, c);
    }

    [Fact]
    public void Stopped_eta_does_not_climb_or_countdown_when_plan_rem_flat()
    {
        var lag = 0f;
        var arrival = -1f;
        var prevPlan = -1f;
        var displayed = -1f;
        var now = 0f;
        var planRem = 1843f;
        var eta = RouteEtaSmooth.Tick(planRem, 2000f, now, ref lag, ref arrival, ref prevPlan, ref displayed);
        for (var i = 0; i < 30; i++)
        {
            now += 1f;
            var next = RouteEtaSmooth.Tick(planRem, paceHintSeconds: null, now, ref lag, ref arrival, ref prevPlan, ref displayed);
            Assert.Equal(eta, next);
        }
    }

    [Fact]
    public void Reset_clears_lag_and_arrival()
    {
        var lag = 40f;
        var arrival = 999f;
        var prevPlan = 50f;
        var displayed = 40f;
        RouteEtaSmooth.Reset(ref lag, ref arrival, ref prevPlan, ref displayed);
        Assert.Equal(0f, lag);
        Assert.Equal(-1f, arrival);
        Assert.Equal(-1f, prevPlan);
        Assert.Equal(-1f, displayed);
    }

    [Fact]
    public void PaceHint_null_when_crawl()
    {
        Assert.Null(RouteEtaSmooth.PaceHintSeconds(1200f, 5f));
        Assert.InRange(RouteEtaSmooth.PaceHintSeconds(1200f, 60f)!.Value, 71f, 73f);
    }
}
