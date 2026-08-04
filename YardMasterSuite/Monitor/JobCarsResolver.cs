using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>Resolves a Job to live <see cref="TrainCar"/> instances (3.1).</summary>
internal static class JobCarsResolver
{
    private const BindingFlags InstanceAll =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public sealed class ResolveResult
    {
        public ResolveResult(Job job, IReadOnlyList<TrainCar> cars, int expectedLogicCars, bool hazmatPresent)
        {
            Job = job;
            Cars = cars;
            ExpectedLogicCars = expectedLogicCars;
            HazmatPresent = hazmatPresent;
        }

        public Job Job { get; }
        public IReadOnlyList<TrainCar> Cars { get; }
        public int ExpectedLogicCars { get; }
        public bool HazmatPresent { get; }
    }

    public static bool TryResolve(Job? job, out ResolveResult? result, out string? error)
    {
        result = null;
        error = null;
        if (job == null)
        {
            error = "no job";
            return false;
        }

        try
        {
            var logicCars = new List<Car>();
            CollectLogicCars(job.tasks, logicCars, depth: 0);
            if (logicCars.Count == 0)
            {
                error = "no job cars";
                return false;
            }

            var trains = new List<TrainCar>(logicCars.Count);
            var hazmat = false;
            for (var i = 0; i < logicCars.Count; i++)
            {
                var logic = logicCars[i];
                if (logic == null)
                {
                    continue;
                }

                if (IsHazmat(logic))
                {
                    hazmat = true;
                }

                var train = LogicCarExtensions.TrainCar(logic);
                if (train == null)
                {
                    error = "car unresolved";
                    return false;
                }

                trains.Add(train);
            }

            if (trains.Count != logicCars.Count)
            {
                error = "partial resolve";
                return false;
            }

            result = new ResolveResult(job, trains, logicCars.Count, hazmat);
            return true;
        }
        catch (Exception ex)
        {
            error = "resolve failed";
            Main.Log($"T2 teleport: resolve fail · {ex.GetType().Name}");
            return false;
        }
    }

    public static float MaxAbsSpeedKmh(IReadOnlyList<TrainCar> cars)
    {
        var max = 0f;
        if (cars == null)
        {
            return max;
        }

        for (var i = 0; i < cars.Count; i++)
        {
            var car = cars[i];
            if (car == null)
            {
                continue;
            }

            try
            {
                var speed = Mathf.Abs(car.GetAbsSpeed());
                // GetAbsSpeed is m/s in DV — convert to km/h for policy gate.
                var kmh = speed * 3.6f;
                if (kmh > max)
                {
                    max = kmh;
                }
            }
            catch
            {
                // ignore one car
            }
        }

        return max;
    }

    private static void CollectLogicCars(object? tasksObj, List<Car> sink, int depth)
    {
        if (tasksObj == null || depth > 12)
        {
            return;
        }

        if (tasksObj is TransportTask transport)
        {
            try
            {
                var carsObj = transport.GetType().GetField("cars", InstanceAll)?.GetValue(transport);
                if (carsObj is IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        if (item is Car car && !sink.Contains(car))
                        {
                            sink.Add(car);
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            return;
        }

        if (tasksObj is SequentialTasks sequential)
        {
            CollectLogicCars(GetMember(sequential, "tasks"), sink, depth + 1);
            return;
        }

        if (tasksObj is ParallelTasks parallel)
        {
            CollectLogicCars(GetMember(parallel, "tasks"), sink, depth + 1);
            return;
        }

        if (tasksObj is Task)
        {
            CollectLogicCars(GetMember(tasksObj, "tasks"), sink, depth + 1);
            CollectLogicCars(GetMember(tasksObj, "cars"), sink, depth + 1);
            return;
        }

        if (tasksObj is IEnumerable enumerable2 && tasksObj is not string)
        {
            foreach (var item in enumerable2)
            {
                CollectLogicCars(item, sink, depth + 1);
            }
        }
    }

    private static object? GetMember(object obj, string name)
    {
        try
        {
            var type = obj.GetType();
            return type.GetField(name, InstanceAll)?.GetValue(obj)
                ?? type.GetProperty(name, InstanceAll)?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsHazmat(Car logic)
    {
        try
        {
            var cargo = logic.CurrentCargoTypeInCar;
            var name = cargo.ToString();
            if (string.IsNullOrEmpty(name) || name.Equals("None", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Empty", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Conservative: Hazardous / Gas / Explosive / Radioactive tokens.
            return name.IndexOf("Hazard", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Explosive", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Radioactive", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Ammonia", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Chlorine", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }
}
