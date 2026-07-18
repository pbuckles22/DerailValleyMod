using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Read-only telemetry from the target car / usable loco train. No game-state writes.
/// Internal — DV CommandTerminal scans public types across mod assemblies.
/// </summary>
internal static class TelemetryReader
{
    private const float LookAtMaxDistanceMeters = 80f;

    private static int _trainLookMask = -1;

    /// <summary>
    /// Car under inspection: standing on it wins; else the car under the crosshair (CMD-01d).
    /// </summary>
    public static TrainCar? TryGetTargetCar()
    {
        var standing = TryGetStandingCar();
        // Skip raycast when standing — selection policy still goes through TargetCarSelection.
        var lookAt = standing == null ? TryGetLookAtCar() : null;
        return TargetCarSelection.Resolve(standing != null, lookAt != null) switch
        {
            TargetCarSource.Standing => standing,
            TargetCarSource.LookAt => lookAt,
            _ => null,
        };
    }

    public static TrainCar? TryGetStandingCar()
    {
        try
        {
            return PlayerManager.Car;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Car under the center of the active camera (train collider layers). Null if none / fail-closed.
    /// </summary>
    public static TrainCar? TryGetLookAtCar()
    {
        try
        {
            var cam = PlayerManager.ActiveCamera;
            if (cam == null)
            {
                return null;
            }

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(ray, out var hit, LookAtMaxDistanceMeters, TrainLookMask()))
            {
                return null;
            }

            return TrainCar.Resolve(hit.collider.transform);
        }
        catch
        {
            return null;
        }
    }

    private static int TrainLookMask()
    {
        if (_trainLookMask < 0)
        {
            _trainLookMask = LayerMask.GetMask("Train_Big_Collider", "Train_Interior");
        }

        return _trainLookMask;
    }

    /// <summary>
    /// True when the target car is on a usable loco train (continuous full links to a loco).
    /// </summary>
    public static bool HasUsableLocoTrain()
    {
        try
        {
            return TryGetUsableConsist() != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Legacy name — same as <see cref="HasUsableLocoTrain"/>.</summary>
    public static bool HasLocoAnchoredTrain() => HasUsableLocoTrain();

    public static bool IsLocalCarVisible() => TryGetTargetCar() != null;

    public static float? TryGetAbsSpeedMetersPerSecond()
    {
        try
        {
            var loco = TryGetUsableLoco();
            return loco?.GetAbsSpeed();
        }
        catch
        {
            return null;
        }
    }

    public static float? TryGetGradePercent()
    {
        try
        {
            var loco = TryGetUsableLoco();
            if (loco == null)
            {
                return null;
            }

            var f = loco.transform.forward;
            return GradeDisplay.PercentFromDirection(f.x, f.y, f.z);
        }
        catch
        {
            return null;
        }
    }

    public static float? TryGetConsistMassKilograms()
    {
        try
        {
            var usable = TryGetUsableConsist();
            if (usable == null || usable.Count == 0)
            {
                return null;
            }

            float total = 0f;
            foreach (var c in usable)
            {
                if (c?.massController != null)
                {
                    total += c.massController.TotalMass;
                }
            }

            return total;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Non-loco cars in the usable consist only.</summary>
    public static int? TryGetConsistCarCount()
    {
        try
        {
            var usable = TryGetUsableConsist();
            if (usable == null)
            {
                return null;
            }

            var count = 0;
            foreach (var c in usable)
            {
                if (c != null && !c.IsLoco)
                {
                    count++;
                }
            }

            return count;
        }
        catch
        {
            return null;
        }
    }

    public static int? TryGetConsistHandbrakeAppliedCount()
    {
        try
        {
            var usable = TryGetUsableConsist();
            if (usable == null)
            {
                return null;
            }

            var positions = new List<float>();
            foreach (var c in usable)
            {
                var brakes = c?.brakeSystem;
                if (brakes == null || !brakes.hasHandbrake)
                {
                    continue;
                }

                positions.Add(brakes.handbrakePosition);
            }

            return HandbrakeDisplay.CountApplied(positions);
        }
        catch
        {
            return null;
        }
    }

    public static float? TryGetBrakePipePressureBar()
    {
        try
        {
            return TryGetTargetCar()?.brakeSystem?.brakePipePressure;
        }
        catch
        {
            return null;
        }
    }

    public static int? TryGetHandbrakeAppliedCount()
    {
        try
        {
            var brakes = TryGetTargetCar()?.brakeSystem;
            if (brakes == null || !brakes.hasHandbrake)
            {
                return null;
            }

            return HandbrakeDisplay.IsApplied(brakes.handbrakePosition) ? 1 : 0;
        }
        catch
        {
            return null;
        }
    }

    public static CouplerLinkStatus? TryGetFrontLinkStatus()
    {
        try
        {
            return TryGetLinkStatus(TryGetTargetCar()?.frontCoupler);
        }
        catch
        {
            return null;
        }
    }

    public static CouplerLinkStatus? TryGetRearLinkStatus()
    {
        try
        {
            return TryGetLinkStatus(TryGetTargetCar()?.rearCoupler);
        }
        catch
        {
            return null;
        }
    }

    public static string CurrentTrainHudLine()
    {
        if (!HasUsableLocoTrain())
        {
            return TrainHudLine.NullLine();
        }

        return TrainHudLine.Format(
            SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
            CarsDisplay.Format(TryGetConsistCarCount()),
            HandbrakeDisplay.FormatTotal(TryGetConsistHandbrakeAppliedCount()));
    }

    public static string? CurrentLocalCarHudLineOrNull()
    {
        var car = TryGetTargetCar();
        if (car == null)
        {
            return null;
        }

        return LocalCarHudLine.Format(
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.FormatHud(TryGetFrontLinkStatus(), TryGetRearLinkStatus()),
            FormatCarNumber(car),
            JobDisplay.Format(TryGetJobId()));
    }

    public static string CurrentHudLine() =>
        CurrentLocalCarHudLineOrNull() is { } local
            ? MonitorHudLine.Join(new[] { CurrentTrainHudLine(), local })
            : CurrentTrainHudLine();

    internal static ConsistDebugSnapshot CurrentConsistDebugSnapshot()
    {
        var usable = HasUsableLocoTrain();
        return new ConsistDebugSnapshot(
            usable,
            CarsDisplay.Format(usable ? TryGetConsistCarCount() : null),
            HandbrakeDisplay.FormatTotal(usable ? TryGetConsistHandbrakeAppliedCount() : null));
    }

    /// <summary>Standing-on-car second bar only (not look-at).</summary>
    internal static LocalCarDebugSnapshot CurrentLocalCarDebugSnapshot() =>
        SnapshotForCar(TryGetStandingCar());

    /// <summary>Look-at second bar only when standing does not win.</summary>
    internal static LocalCarDebugSnapshot CurrentLookAtDebugSnapshot()
    {
        if (TryGetStandingCar() != null)
        {
            return HiddenLocalCarSnapshot();
        }

        return SnapshotForCar(TryGetLookAtCar());
    }

    /// <summary>Target-car coupler marks (standing wins over look-at).</summary>
    internal static CouplerDebugSnapshot CurrentCouplerDebugSnapshot()
    {
        if (TryGetTargetCar() == null)
        {
            return new CouplerDebugSnapshot(visible: false, coupling: "— Couplers");
        }

        return new CouplerDebugSnapshot(
            visible: true,
            CouplingDisplay.Format(TryGetFrontLinkStatus(), TryGetRearLinkStatus()));
    }

    private static LocalCarDebugSnapshot SnapshotForCar(TrainCar? car)
    {
        if (car == null)
        {
            return HiddenLocalCarSnapshot();
        }

        // Callers only pass the active target (standing, or look-at when not standing),
        // so TryGet* helpers that read TryGetTargetCar() match this car.
        return new LocalCarDebugSnapshot(
            visible: true,
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.Format(TryGetFrontLinkStatus(), TryGetRearLinkStatus()),
            FormatCarNumber(car),
            JobDisplay.Format(TryGetJobId()));
    }

    private static LocalCarDebugSnapshot HiddenLocalCarSnapshot() =>
        new(
            visible: false,
            pipe: "— Pipe",
            handbrake: "— Handbrake",
            coupling: "— Couplers",
            carNumber: CarNumberDisplay.NotOnTrainLabel,
            job: "— Job");

    internal static IntegrityDebugSnapshot CurrentIntegrityDebugSnapshot()
    {
        var onCar = IsLocalCarVisible();
        return new IntegrityDebugSnapshot(
            onCar,
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.Format(TryGetFrontLinkStatus(), TryGetRearLinkStatus()));
    }

    private static string FormatCarNumber(TrainCar car)
    {
        if (car.IsLoco)
        {
            return CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: null);
        }

        var usable = TryGetUsableConsist();
        if (usable == null || !usable.Contains(car))
        {
            return CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: null);
        }

        var loco = TryGetUsableLoco();
        var set = car.trainset;
        if (loco == null || set?.cars == null)
        {
            return CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: null);
        }

        var lo = loco.indexInTrainset < car.indexInTrainset
            ? loco.indexInTrainset
            : car.indexInTrainset;
        var hi = loco.indexInTrainset < car.indexInTrainset
            ? car.indexInTrainset
            : loco.indexInTrainset;
        var freight = 0;
        for (var i = lo; i <= hi; i++)
        {
            var c = set.cars[i];
            if (c != null && !c.IsLoco && usable.Contains(c))
            {
                freight++;
            }
        }

        return CarNumberDisplay.Format(
            isLoco: false,
            freightNumberFromLoco: freight > 0 ? freight : null);
    }

    private static string? TryGetJobId()
    {
        try
        {
            var logicCar = TryGetTargetCar()?.logicCar;
            if (logicCar == null || JobsManager.Instance == null)
            {
                return null;
            }

            var job = JobsManager.Instance.GetJobOfCar(logicCar);
            var id = job?.ID?.Trim();
            return string.IsNullOrEmpty(id) ? null : id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Usable cars = fully-linked component containing the target car and at least one loco.
    /// Incomplete links (loose chain, missing hose, closed cock) break the train.
    /// Missing loco↔loco MU is a yellow warning only and does not break this component.
    /// </summary>
    private static HashSet<TrainCar>? TryGetUsableConsist()
    {
        var target = TryGetTargetCar();
        if (target == null)
        {
            return null;
        }

        var component = CollectFullyLinkedComponent(target);
        foreach (var c in component)
        {
            if (c != null && c.IsLoco)
            {
                return component;
            }
        }

        return null;
    }

    /// <summary>Nearest loco in the usable component (stable for multi-loco consists).</summary>
    private static TrainCar? TryGetUsableLoco()
    {
        var target = TryGetTargetCar();
        var usable = TryGetUsableConsist();
        if (target == null || usable == null)
        {
            return null;
        }

        TrainCar? best = null;
        var bestDist = int.MaxValue;
        foreach (var c in usable)
        {
            if (c == null || !c.IsLoco)
            {
                continue;
            }

            var dist = c.indexInTrainset - target.indexInTrainset;
            if (dist < 0)
            {
                dist = -dist;
            }

            if (best == null
                || dist < bestDist
                || (dist == bestDist && c.indexInTrainset < best.indexInTrainset))
            {
                bestDist = dist;
                best = c;
            }
        }

        return best;
    }

    private static HashSet<TrainCar> CollectFullyLinkedComponent(TrainCar start)
    {
        var visited = new HashSet<TrainCar>();
        var stack = new Stack<TrainCar>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            var car = stack.Pop();
            if (car == null || !visited.Add(car))
            {
                continue;
            }

            TryWalk(car.frontCoupler, stack);
            TryWalk(car.rearCoupler, stack);
        }

        return visited;
    }

    private static void TryWalk(Coupler? coupler, Stack<TrainCar> stack)
    {
        var status = TryGetLinkStatus(coupler);
        if (status is null || !CouplingLink.IsUsable(status.Value))
        {
            return;
        }

        var other = coupler!.GetCoupled() ?? coupler.coupledTo;
        var otherCar = other?.train;
        if (otherCar != null)
        {
            stack.Push(otherCar);
        }
    }

    private static CouplerLinkStatus? TryGetLinkStatus(Coupler? coupler)
    {
        if (coupler == null)
        {
            return null;
        }

        var other = coupler.GetCoupled() ?? coupler.coupledTo;
        var mechanicallyCoupled = coupler.IsCoupled();
        // Screw may report tight on only one side of the pair.
        var tightened = mechanicallyCoupled
            && (coupler.IsTightened() || (other != null && other.IsTightened()));
        var airHoseConnected = IsAirHoseConnected(coupler);
        var cocksOpen = AreCocksOpenBothSides(coupler);
        // Blue MU only when both ends have an adapter (loco↔loco). Missing MU = yellow warning, still usable.
        var mu = TryGetMuAdapter(coupler);
        var otherMu = other == null ? null : TryGetMuAdapter(other);
        var muPresent = mu != null && mu.IsInitialized
            && otherMu != null && otherMu.IsInitialized;
        var muConnected = mu != null && otherMu != null
            && mu.IsInitialized && otherMu.IsInitialized
            && mu.IsConnected && otherMu.IsConnected;
        return CouplingLink.Resolve(
            mechanicallyCoupled,
            tightened,
            airHoseConnected,
            cocksOpen,
            muPresent,
            muConnected);
    }

    private static bool AreCocksOpenBothSides(Coupler coupler)
    {
        if (!coupler.IsCockOpen)
        {
            return false;
        }

        var other = coupler.GetCoupled() ?? coupler.coupledTo;
        return other != null && other.IsCockOpen;
    }

    private static bool IsAirHoseConnected(Coupler coupler)
    {
        if (coupler.GetAirHoseConnectedTo() != null)
        {
            return true;
        }

        var hoseAndCock = coupler.hoseAndCock;
        if (hoseAndCock != null && hoseAndCock.IsHoseConnected)
        {
            return true;
        }

        var adapter = coupler.visualCoupler?.hoseAdapter;
        return adapter != null && adapter.IsConnected;
    }

    private static CouplingHoseMultipleUnitAdapter? TryGetMuAdapter(Coupler coupler)
    {
        var visual = coupler.visualCoupler;
        return visual == null
            ? null
            : visual.GetComponentInChildren<CouplingHoseMultipleUnitAdapter>(true);
    }
}
