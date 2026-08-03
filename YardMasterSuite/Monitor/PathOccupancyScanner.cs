using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// One-pass car → track occupancy for Align #4. Call only on Set dest / Recheck / Align —
/// never per-track and never on the HUD tick.
/// </summary>
internal static class PathOccupancyScanner
{
    /// <summary>Last snapshot size (desk / Player.log).</summary>
    public static int LastCarCount { get; private set; }

    public static int LastOccupiedTracks { get; private set; }

    public static HashSet<string> SnapshotOccupiedTrackKeys()
    {
        var keys = new List<string?>(256);
        LastCarCount = 0;
        try
        {
            var cars = Object.FindObjectsOfType<TrainCar>();
            if (cars == null || cars.Length == 0)
            {
                LastOccupiedTracks = 0;
                return PathRouteConstraints.OccupiedSet(keys);
            }

            var own = PlayerTrainset();
            foreach (var car in cars)
            {
                if (car == null)
                {
                    continue;
                }

                LastCarCount++;
                if (own != null && car.trainset == own)
                {
                    continue; // own consist must not block origin / our rails
                }

                AddTrackKeys(keys, car);
            }
        }
        catch
        {
            // fail open to empty — pathfind without occupancy rather than throw
        }

        var set = PathRouteConstraints.OccupiedSet(keys);
        LastOccupiedTracks = set.Count;
        return set;
    }

    private static object? PlayerTrainset()
    {
        try
        {
            var seed = PlayerManager.Car ?? PlayerManager.LastLoco;
            return seed?.trainset;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Logic + both bogies — keys can disagree at junctions; any hit blocks.</summary>
    private static void AddTrackKeys(List<string?> keys, TrainCar car)
    {
        try
        {
            keys.Add(PathGraphBuilder.TrackKey(car.logicCar?.CurrentTrack));
            keys.Add(PathGraphBuilder.TrackKey(car.FrontBogie?.track));
            keys.Add(PathGraphBuilder.TrackKey(car.RearBogie?.track));
        }
        catch
        {
            // ignore this car
        }
    }
}
