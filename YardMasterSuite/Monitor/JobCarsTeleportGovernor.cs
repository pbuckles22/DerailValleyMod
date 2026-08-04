using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>ThreeGate + TeleportTrainset for job cars place mode (3.1).</summary>
internal static class JobCarsTeleportGovernor
{
    public static string BeginPlaceForJob(Job? job)
    {
        if (job == null)
        {
            return "T2 teleport: no job";
        }

        if (!JobCarsResolver.TryResolve(job, out var resolved, out var error) || resolved == null)
        {
            return "T2 teleport: " + (error ?? "resolve failed");
        }

        JobCarsPlaceSession.Begin(job.ID?.Trim() ?? "", resolved.ExpectedLogicCars);
        var line = $"T2 teleport: place · {resolved.Cars.Count} cars · {job.ID}";
        Main.Log(line);
        return line;
    }

    public static string CancelPlace()
    {
        JobCarsPlaceSession.Clear();
        Main.Log("T2 teleport: place cancelled");
        return "place cancelled";
    }

    /// <summary>
    /// Confirm place: resolve job cars again, snap target from session track id, TeleportTrainset.
    /// </summary>
    public static string ConfirmPlace(MonoBehaviour host)
    {
        if (host == null)
        {
            return "T2 teleport: no host";
        }

        if (!JobCarsPlaceSession.IsActive)
        {
            return "T2 teleport: place inactive";
        }

        var jobId = JobCarsPlaceSession.JobId;
        Job? job = null;
        var candidates = SwitchListJobReader.ListCandidateJobs();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (string.Equals(candidates[i].ID, jobId, System.StringComparison.Ordinal))
            {
                job = candidates[i];
                break;
            }
        }

        if (!JobCarsResolver.TryResolve(job, out var resolved, out var error) || resolved == null)
        {
            return "T2 teleport: " + (error ?? "resolve failed");
        }

        var targetKey = JobCarsPlaceSession.TargetTrackId;
        if (string.IsNullOrEmpty(targetKey) || !TryGetRailTrack(targetKey!, out var rail) || rail == null)
        {
            return "T2 teleport: " + JobCarsTeleportPolicy.FormatAbort(JobCarsTeleportAbort.NoTarget);
        }

        var speed = JobCarsResolver.MaxAbsSpeedKmh(resolved.Cars);
        var abort = JobCarsTeleportPolicy.Evaluate(
            hasJob: true,
            expectedCarCount: resolved.ExpectedLogicCars,
            resolvedCarCount: resolved.Cars.Count,
            maxAbsSpeedKmh: speed,
            isTeleporting: IsTeleportingTrain(),
            hasTargetTrack: true,
            hazmatPresent: resolved.HazmatPresent);

        if (!JobCarsTeleportPolicy.CanTeleport(abort))
        {
            var blocked = "T2 teleport: abort · " + JobCarsTeleportPolicy.FormatAbort(abort);
            Main.Log(blocked);
            return blocked;
        }

        var playerPos = PlayerManager.PlayerTransform != null
            ? PlayerManager.PlayerTransform.position
            : resolved.Cars[0].transform.position;
        Vector3 aimPos;
        if (JobCarsPlaceSession.TryGetAimPoint(out var ax, out var ay, out var az))
        {
            aimPos = new Vector3(ax, ay, az);
        }
        else
        {
            aimPos = playerPos;
        }

        var closest = RailTrack.GetClosestPoint(rail, aimPos, 0f);
        if (closest.Item1 == null)
        {
            return "T2 teleport: no snap point";
        }

        // Aim point on track plane — look-at location, not feet.
        var target = new Vector3(aimPos.x, rail.transform.position.y, aimPos.z);
        var forceDir = JobCarsPlaceSession.ForceRegularDirection;
        var cars = new List<TrainCar>(resolved.Cars);
        var hostRef = host;

        var result = ThreeGate.TryApply(
            integrityOk: cars.Count > 0,
            stateRegistryOk: JobsManager.Instance != null,
            safetyOk: true,
            softWrite: () =>
            {
                hostRef.StartCoroutine(TeleportThenClear(cars, target, forceDir));
                return true;
            });

        if (!result.Applied)
        {
            var fail = "T2 teleport: ThreeGate " + result.AbortReason;
            Main.Log(fail);
            return fail;
        }

        var ok = $"T2 teleport: started · {cars.Count} cars → {targetKey} · aim=({aimPos.x:0.0},{aimPos.z:0.0})";
        Main.Log(ok);
        return ok;
    }

    private static IEnumerator TeleportThenClear(List<TrainCar> cars, Vector3 target, bool forceRegularDirection)
    {
        yield return TrainCarTeleporter.TeleportTrainset(cars, target, forceRegularDirection);
        JobCarsPlaceSession.Clear();
        Main.Log("T2 teleport: complete");
    }

    private static bool IsTeleportingTrain()
    {
        try
        {
            var field = typeof(TrainCarTeleporter).GetField(
                "isTeleportingTrain",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field?.GetValue(null) is bool busy)
            {
                return busy;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static bool TryGetRailTrack(string trackKey, out RailTrack? rail)
    {
        rail = null;
        try
        {
            var tracks = RailTrackRegistry.RailTracks;
            if (tracks == null)
            {
                return false;
            }

            foreach (var t in tracks)
            {
                if (t == null)
                {
                    continue;
                }

                var key = PathGraphBuilder.TrackKey(t);
                if (string.Equals(key, trackKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    rail = t;
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
