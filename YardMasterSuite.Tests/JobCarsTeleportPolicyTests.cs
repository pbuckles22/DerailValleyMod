using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobCarsTeleportPolicyTests
{
    [Fact]
    public void Evaluate_ok_when_all_gates_pass()
    {
        var abort = JobCarsTeleportPolicy.Evaluate(
            hasJob: true,
            expectedCarCount: 3,
            resolvedCarCount: 3,
            maxAbsSpeedKmh: 0f,
            isTeleporting: false,
            hasTargetTrack: true,
            hazmatPresent: false);
        Assert.Equal(JobCarsTeleportAbort.None, abort);
        Assert.True(JobCarsTeleportPolicy.CanTeleport(abort));
    }

    [Fact]
    public void Evaluate_fail_closed_cases()
    {
        Assert.Equal(
            JobCarsTeleportAbort.NoJob,
            JobCarsTeleportPolicy.Evaluate(false, 3, 3, 0f, false, true, false));
        Assert.Equal(
            JobCarsTeleportAbort.NoCars,
            JobCarsTeleportPolicy.Evaluate(true, 0, 0, 0f, false, true, false));
        Assert.Equal(
            JobCarsTeleportAbort.PartialResolve,
            JobCarsTeleportPolicy.Evaluate(true, 3, 2, 0f, false, true, false));
        Assert.Equal(
            JobCarsTeleportAbort.Moving,
            JobCarsTeleportPolicy.Evaluate(true, 3, 3, 5f, false, true, false));
        Assert.Equal(
            JobCarsTeleportAbort.BusyTeleporting,
            JobCarsTeleportPolicy.Evaluate(true, 3, 3, 0f, true, true, false));
        Assert.Equal(
            JobCarsTeleportAbort.NoTarget,
            JobCarsTeleportPolicy.Evaluate(true, 3, 3, 0f, false, false, false));
        Assert.Equal(
            JobCarsTeleportAbort.Hazmat,
            JobCarsTeleportPolicy.Evaluate(true, 3, 3, 0f, false, true, true));
    }

    [Fact]
    public void FormatPlaceChip_ok_and_blocked()
    {
        Assert.Equal(
            "PLACE OK · 3 cars · FF-C3O",
            JobCarsTeleportPolicy.FormatPlaceChip(true, 3, "FF-C3O", JobCarsTeleportAbort.None));
        Assert.Contains(
            "BLOCKED",
            JobCarsTeleportPolicy.FormatPlaceChip(true, 3, null, JobCarsTeleportAbort.NoTarget));
        Assert.Equal("", JobCarsTeleportPolicy.FormatPlaceChip(false, 3, "X", JobCarsTeleportAbort.None));
    }
}

public class JobCarsPlaceSessionTests
{
    [Fact]
    public void Begin_toggle_clear()
    {
        JobCarsPlaceSession.Clear();
        JobCarsPlaceSession.Begin("FF-FH-11", 4);
        Assert.True(JobCarsPlaceSession.IsActive);
        Assert.Equal("FF-FH-11", JobCarsPlaceSession.JobId);
        Assert.Equal(4, JobCarsPlaceSession.ExpectedCars);
        Assert.True(JobCarsPlaceSession.ForceRegularDirection);

        JobCarsPlaceSession.SetTargetTrack(" FF-A2P ");
        Assert.Equal("FF-A2P", JobCarsPlaceSession.TargetTrackId);
        JobCarsPlaceSession.SetAimPoint(10f, 20f, 30f);
        Assert.True(JobCarsPlaceSession.HasAimPoint);
        Assert.True(JobCarsPlaceSession.TryGetAimPoint(out var ax, out var ay, out var az));
        Assert.Equal(10f, ax);
        Assert.Equal(20f, ay);
        Assert.Equal(30f, az);
        JobCarsPlaceSession.ToggleFacing();
        Assert.False(JobCarsPlaceSession.ForceRegularDirection);

        JobCarsPlaceSession.ClearAim();
        Assert.False(JobCarsPlaceSession.HasAimPoint);
        Assert.Null(JobCarsPlaceSession.TargetTrackId);
        Assert.True(JobCarsPlaceSession.IsActive);

        JobCarsPlaceSession.Clear();
        Assert.False(JobCarsPlaceSession.IsActive);
    }
}

public class StationSnapSessionTests
{
    [Fact]
    public void Capture_and_return()
    {
        StationSnapSession.Clear();
        Assert.False(StationSnapSession.HasReturnPoint);
        StationSnapSession.CaptureReturn(1f, 2f, 3f);
        Assert.True(StationSnapSession.TryGetReturn(out var x, out var y, out var z));
        Assert.Equal(1f, x);
        Assert.Equal(2f, y);
        Assert.Equal(3f, z);
        StationSnapSession.Clear();
        Assert.False(StationSnapSession.HasReturnPoint);
    }
}
