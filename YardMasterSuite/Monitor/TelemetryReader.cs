using System;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using DV.ThingTypes;
using LocoSim.Implementations;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Read-only telemetry from the target car / usable loco train. No game-state writes.
/// Internal — DV CommandTerminal scans public types across mod assemblies.
/// </summary>
internal static class TelemetryReader
{
    private static int _trainLookMask = -1;

    // Per HUD refresh: cache standing / look-at / target / loco so one tick does not re-spherecast.
    private static bool _tickActive;
    private static bool _standingResolved;
    private static TrainCar? _standingCar;
    private static bool _lookAtResolved;
    private static TrainCar? _lookAtCar;
    private static bool _targetResolved;
    private static TrainCar? _targetCar;
    private static bool _usableLocoResolved;
    private static TrainCar? _usableLoco;

    /// <summary>Cached amp/load field map per <see cref="SimComponent"/> CLR type.</summary>
    private static readonly Dictionary<Type, LoadFieldMap> LoadFieldCache = new();

    /// <summary>Cached motor-status fields for private <see cref="TractionMotorSet"/> members.</summary>
    private static MotorSetFieldMap? _motorSetFields;

    /// <summary>Call once at the start of each Monitor HUD refresh.</summary>
    public static void BeginHudTick()
    {
        _tickActive = true;
        _standingResolved = false;
        _lookAtResolved = false;
        _targetResolved = false;
        _usableLocoResolved = false;
        _standingCar = null;
        _lookAtCar = null;
        _targetCar = null;
        _usableLoco = null;
    }

    public static void EndHudTick() => _tickActive = false;

    /// <summary>
    /// Car under inspection: look-at wins; standing is the fallback when not looking at a car.
    /// </summary>
    public static TrainCar? TryGetTargetCar()
    {
        if (_tickActive && _targetResolved)
        {
            return _targetCar;
        }

        var standing = TryGetStandingCar();
        var lookAt = TryGetLookAtCar();
        var resolved = TargetCarSelection.Resolve(standing != null, lookAt != null) switch
        {
            TargetCarSource.Standing => standing,
            TargetCarSource.LookAt => lookAt,
            _ => null,
        };

        if (_tickActive)
        {
            _targetCar = resolved;
            _targetResolved = true;
        }

        return resolved;
    }

    public static TrainCar? TryGetStandingCar()
    {
        if (_tickActive && _standingResolved)
        {
            return _standingCar;
        }

        TrainCar? car = null;
        try
        {
            car = PlayerManager.Car;
        }
        catch
        {
            car = null;
        }

        if (_tickActive)
        {
            _standingCar = car;
            _standingResolved = true;
        }

        return car;
    }

    /// <summary>
    /// Car under the center of the active camera (train collider layers). Null if none / fail-closed.
    /// QOL-06: spherecast out to <see cref="LookAtTargeting.MaxDistanceMeters"/>.
    /// </summary>
    public static TrainCar? TryGetLookAtCar()
    {
        if (_tickActive && _lookAtResolved)
        {
            return _lookAtCar;
        }

        TrainCar? car = null;
        try
        {
            var cam = PlayerManager.ActiveCamera;
            if (cam != null)
            {
                var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.SphereCast(
                        ray,
                        LookAtTargeting.SphereRadiusMeters,
                        out var hit,
                        LookAtTargeting.MaxDistanceMeters,
                        TrainLookMask()))
                {
                    car = TrainCar.Resolve(hit.collider.transform);
                }
            }
        }
        catch
        {
            car = null;
        }

        if (_tickActive)
        {
            _lookAtCar = car;
            _lookAtResolved = true;
        }

        return car;
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

    /// <summary>
    /// Usable loco-train gadget bar, or null when hidden (no red dash wall — story 4.3).
    /// </summary>
    public static string? CurrentTrainHudLineOrNull()
    {
        if (!HasUsableLocoTrain())
        {
            return null;
        }

        return TrainHudLine.Format(
            SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
            CarsDisplay.Format(TryGetConsistCarCount()),
            HandbrakeDisplay.FormatTotal(TryGetConsistHandbrakeAppliedCount()),
            LoadDisplay.FormatHud(TryGetLoadPercent()),
            MotorDisplay.FormatHud(TryGetMotorStatus()));
    }

    /// <summary>Legacy join helper — empty when top bar is hidden.</summary>
    public static string CurrentTrainHudLine() =>
        CurrentTrainHudLineOrNull() ?? string.Empty;

    /// <summary>Lead usable loco traction load as percent of max amps (null if unavailable).</summary>
    public static float? TryGetLoadPercent()
    {
        try
        {
            var flow = TryGetUsableLoco()?.SimController?.SimulationFlow;
            return flow == null ? null : ReadLoadPercent(flow);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Lead usable loco TM cab status (null if unavailable).</summary>
    public static MotorStatus? TryGetMotorStatus()
    {
        try
        {
            var flow = TryGetUsableLoco()?.SimController?.SimulationFlow;
            return flow == null ? null : ReadMotorStatus(flow);
        }
        catch
        {
            return null;
        }
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
            JobDisplay.Format(TryGetJobId()),
            TryGetCargoLabel(car),
            TryGetLocoTypeLabel(car));
    }

    public static string CurrentHudLine()
    {
        var train = CurrentTrainHudLineOrNull();
        var local = CurrentLocalCarHudLineOrNull();
        if (train != null && local != null)
        {
            return MonitorHudLine.Join(new[] { train, local });
        }

        return train ?? local ?? string.Empty;
    }

    internal static ConsistDebugSnapshot CurrentConsistDebugSnapshot()
    {
        var usable = HasUsableLocoTrain();
        return new ConsistDebugSnapshot(
            usable,
            CarsDisplay.Format(usable ? TryGetConsistCarCount() : null),
            HandbrakeDisplay.FormatTotal(usable ? TryGetConsistHandbrakeAppliedCount() : null));
    }

    internal static PowerDebugSnapshot CurrentPowerDebugSnapshot()
    {
        var usable = HasUsableLocoTrain();
        return new PowerDebugSnapshot(
            usable,
            usable ? LoadDisplay.Format(TryGetLoadPercent()) : LoadDisplay.Format(null),
            usable ? MotorDisplay.Format(TryGetMotorStatus()) : MotorDisplay.Format(null));
    }

    /// <summary>Standing fallback second bar (hidden when look-at wins).</summary>
    internal static LocalCarDebugSnapshot CurrentLocalCarDebugSnapshot()
    {
        var standing = TryGetStandingCar();
        var lookAt = TryGetLookAtCar();
        if (TargetCarSelection.Resolve(standing != null, lookAt != null) != TargetCarSource.Standing)
        {
            return HiddenLocalCarSnapshot();
        }

        return SnapshotForCar(standing);
    }

    /// <summary>Look-at second bar when it is the active target (wins over standing).</summary>
    internal static LocalCarDebugSnapshot CurrentLookAtDebugSnapshot()
    {
        var standing = TryGetStandingCar();
        var lookAt = TryGetLookAtCar();
        if (TargetCarSelection.Resolve(standing != null, lookAt != null) != TargetCarSource.LookAt)
        {
            return HiddenLocalCarSnapshot();
        }

        return SnapshotForCar(lookAt);
    }

    /// <summary>Target-car coupler marks (look-at wins over standing).</summary>
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

        // Callers only pass the active target, so TryGet* helpers that read TryGetTargetCar() match.
        return new LocalCarDebugSnapshot(
            visible: true,
            BrakePipeDisplay.FormatBar(TryGetBrakePipePressureBar()),
            HandbrakeDisplay.FormatCount(TryGetHandbrakeAppliedCount()),
            CouplingDisplay.Format(TryGetFrontLinkStatus(), TryGetRearLinkStatus()),
            FormatCarNumber(car),
            JobDisplay.Format(TryGetJobId()),
            TryGetCargoLabel(car),
            TryGetLocoTypeLabel(car));
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

    private static string? TryGetCargoLabel(TrainCar car)
    {
        try
        {
            var cargo = car.LoadedCargo;
            var name = cargo == CargoType.None ? null : cargo.ToString();
            return CargoDisplay.Format(car.IsLoco, name);
        }
        catch
        {
            return CargoDisplay.Format(car.IsLoco, null);
        }
    }

    private static string? TryGetLocoTypeLabel(TrainCar car)
    {
        try
        {
            if (!car.IsLoco)
            {
                return null;
            }

            var id = car.carLivery?.parentType?.id ?? car.carLivery?.id;
            return LocoTypeDisplay.Format(id);
        }
        catch
        {
            return null;
        }
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
        if (_tickActive && _usableLocoResolved)
        {
            return _usableLoco;
        }

        TrainCar? best = null;
        try
        {
            var target = TryGetTargetCar();
            var usable = TryGetUsableConsist();
            if (target != null && usable != null)
            {
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
            }
        }
        catch
        {
            best = null;
        }

        if (_tickActive)
        {
            _usableLoco = best;
            _usableLocoResolved = true;
        }

        return best;
    }

    /// <summary>
    /// DE2/DE6 expose amps on TractionMotor, TractionMotorSet, and/or TractionGenerator.
    /// PortDefinition.ID strings are asset-defined and unreliable — match CLR field names instead.
    /// </summary>
    private static float? ReadLoadPercent(SimulationFlow flow)
    {
        if (flow?.OrderedSimComps == null)
        {
            return null;
        }

        foreach (var comp in flow.OrderedSimComps)
        {
            if (comp == null)
            {
                continue;
            }

            var fromComp = ReadLoadPercentFromComponent(comp);
            if (fromComp != null)
            {
                return fromComp;
            }
        }

        return null;
    }

    private static MotorStatus? ReadMotorStatus(SimulationFlow flow)
    {
        if (flow?.OrderedSimComps == null)
        {
            return null;
        }

        foreach (var comp in flow.OrderedSimComps)
        {
            if (comp == null)
            {
                continue;
            }

            var fromComp = ReadMotorStatusFromComponent(comp);
            if (fromComp != null)
            {
                return fromComp;
            }
        }

        return null;
    }

    private static MotorStatus? ReadMotorStatusFromComponent(SimComponent comp)
    {
        if (comp is TractionMotor tm)
        {
            return MotorDisplay.StatusFromSignals(
                SafePortValue(tm.tmsStateReadOut),
                SafePortReferenceValue(tm.temperature),
                SafeFloat(tm.overheatingTemperatureThreshold),
                SafePortValue(tm.workingTractionMotorsReadOut),
                tm.numberOfTractionMotors);
        }

        if (comp is TractionMotorSet set)
        {
            return ReadMotorStatusFromMotorSet(set);
        }

        return null;
    }

    private static MotorStatus? ReadMotorStatusFromMotorSet(TractionMotorSet set)
    {
        var map = GetMotorSetFieldMap();
        if (map is null)
        {
            return null;
        }

        return MotorDisplay.StatusFromSignals(
            ReadPortField(set, map.Value.TmsState),
            ReadPortReferenceField(set, map.Value.Temp),
            ReadFloatField(set, map.Value.OverheatThreshold),
            ReadPortField(set, map.Value.Working),
            ReadIntAsFloatField(set, map.Value.NumberOfMotors));
    }

    private static MotorSetFieldMap? GetMotorSetFieldMap()
    {
        if (_motorSetFields is not null)
        {
            return _motorSetFields;
        }

        var type = typeof(TractionMotorSet);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var map = new MotorSetFieldMap(
            type.GetField("tmsStateReadOut", flags),
            type.GetField("tmTempReader", flags),
            type.GetField("overheatingTemperatureThreshold", flags),
            type.GetField("workingTractionMotorsReadOut", flags),
            type.GetField("numberOfTractionMotors", flags));
        if (!map.HasRequired)
        {
            return null;
        }

        _motorSetFields = map;
        return map;
    }

    private static float? ReadIntAsFloatField(SimComponent comp, FieldInfo? field)
    {
        if (field == null)
        {
            return null;
        }

        try
        {
            return field.GetValue(comp) is int n ? n : null;
        }
        catch
        {
            return null;
        }
    }

    private readonly struct MotorSetFieldMap
    {
        public MotorSetFieldMap(
            FieldInfo? tmsState,
            FieldInfo? temp,
            FieldInfo? overheatThreshold,
            FieldInfo? working,
            FieldInfo? numberOfMotors)
        {
            TmsState = tmsState;
            Temp = temp;
            OverheatThreshold = overheatThreshold;
            Working = working;
            NumberOfMotors = numberOfMotors;
        }

        public FieldInfo? TmsState { get; }
        public FieldInfo? Temp { get; }
        public FieldInfo? OverheatThreshold { get; }
        public FieldInfo? Working { get; }
        public FieldInfo? NumberOfMotors { get; }

        public bool HasRequired =>
            TmsState != null && Temp != null && OverheatThreshold != null;
    }

    private static float? ReadLoadPercentFromComponent(SimComponent comp)
    {
        if (comp is TractionMotor tm)
        {
            var normalized = SafePortValue(tm.ampsNormalizedReadOut);
            if (normalized != null)
            {
                return LoadDisplay.PercentFromNormalized(normalized);
            }

            var fromAmps = LoadDisplay.PercentFromAmps(
                SafePortValue(tm.ampsReadOut),
                SafePortValue(tm.maxAmpsReadOut));
            if (fromAmps != null)
            {
                return fromAmps;
            }

            return LoadDisplay.PercentFromNormalized(SafePortValue(tm.loadOnGeneratorReadOut));
        }

        var map = GetOrBuildLoadFieldMap(comp.GetType());
        if (!map.HasAny)
        {
            return null;
        }

        float? ampsNormalized = ReadPortField(comp, map.AmpsNormalized);
        float? amps = ReadPortField(comp, map.Amps);
        float? maxAmps = ReadPortField(comp, map.MaxAmps);
        float? ampsPerTm = ReadPortField(comp, map.AmpsPerTm);
        float? maxPerTm = ReadPortField(comp, map.MaxPerTm);
        float? totalAmps = ReadPortField(comp, map.TotalAmps) ?? ReadPortReferenceField(comp, map.TotalAmpsRef);
        float? working = ReadPortField(comp, map.Working);
        float? loadOnGenerator = ReadPortField(comp, map.LoadOnGenerator);
        float? maxAmpsConst = ReadFloatField(comp, map.MaxAmpsConst);

        if (ampsNormalized != null)
        {
            return LoadDisplay.PercentFromNormalized(ampsNormalized);
        }

        var perTm = LoadDisplay.PercentFromAmps(ampsPerTm, maxPerTm);
        if (perTm != null)
        {
            return perTm;
        }

        var direct = LoadDisplay.PercentFromAmps(amps ?? totalAmps, maxAmps ?? maxAmpsConst);
        if (direct != null)
        {
            return direct;
        }

        if (totalAmps != null && maxPerTm != null && working is > 0f)
        {
            return LoadDisplay.PercentFromAmps(totalAmps, maxPerTm.Value * working.Value);
        }

        return LoadDisplay.PercentFromNormalized(loadOnGenerator);
    }

    private static LoadFieldMap GetOrBuildLoadFieldMap(Type type)
    {
        if (LoadFieldCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        FieldInfo? ampsNormalized = null;
        FieldInfo? amps = null;
        FieldInfo? maxAmps = null;
        FieldInfo? ampsPerTm = null;
        FieldInfo? maxPerTm = null;
        FieldInfo? totalAmps = null;
        FieldInfo? totalAmpsRef = null;
        FieldInfo? working = null;
        FieldInfo? loadOnGenerator = null;
        FieldInfo? maxAmpsConst = null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var field in type.GetFields(flags))
        {
            var name = field.Name;
            if (field.FieldType == typeof(Port))
            {
                if (NameHas(name, "ampsNormalized"))
                {
                    ampsNormalized = field;
                }
                else if (NameHas(name, "loadOnGenerator"))
                {
                    loadOnGenerator = field;
                }
                else if (NameHas(name, "maxAmpsPerTM") || NameHas(name, "maxAmpsPerTm"))
                {
                    maxPerTm = field;
                }
                else if (NameHas(name, "ampsPerTM") || NameHas(name, "ampsPerTm"))
                {
                    ampsPerTm = field;
                }
                else if (NameHas(name, "maxAmpsReadOut") || name.Equals("maxAmps", StringComparison.Ordinal))
                {
                    maxAmps = field;
                }
                else if (name.Equals("ampsReadOut", StringComparison.Ordinal))
                {
                    amps = field;
                }
                else if (NameHas(name, "totalAmps"))
                {
                    totalAmps = field;
                }
                else if (NameHas(name, "workingTractionMotors"))
                {
                    working = field;
                }
            }
            else if (field.FieldType == typeof(PortReference) && NameHas(name, "totalAmps"))
            {
                totalAmpsRef = field;
            }
            else if (field.FieldType == typeof(float) && name.Equals("maxAmps", StringComparison.Ordinal))
            {
                maxAmpsConst = field;
            }
        }

        var map = new LoadFieldMap(
            ampsNormalized,
            amps,
            maxAmps,
            ampsPerTm,
            maxPerTm,
            totalAmps,
            totalAmpsRef,
            working,
            loadOnGenerator,
            maxAmpsConst);
        LoadFieldCache[type] = map;
        return map;
    }

    private static float? ReadPortField(SimComponent comp, FieldInfo? field)
    {
        if (field == null)
        {
            return null;
        }

        try
        {
            return SafePortValue((Port?)field.GetValue(comp));
        }
        catch
        {
            return null;
        }
    }

    private static float? ReadPortReferenceField(SimComponent comp, FieldInfo? field)
    {
        if (field == null)
        {
            return null;
        }

        try
        {
            return SafePortReferenceValue((PortReference?)field.GetValue(comp));
        }
        catch
        {
            return null;
        }
    }

    private static float? ReadFloatField(SimComponent comp, FieldInfo? field)
    {
        if (field == null)
        {
            return null;
        }

        try
        {
            return SafeFloat((float)field.GetValue(comp)!);
        }
        catch
        {
            return null;
        }
    }

    private readonly struct LoadFieldMap
    {
        public LoadFieldMap(
            FieldInfo? ampsNormalized,
            FieldInfo? amps,
            FieldInfo? maxAmps,
            FieldInfo? ampsPerTm,
            FieldInfo? maxPerTm,
            FieldInfo? totalAmps,
            FieldInfo? totalAmpsRef,
            FieldInfo? working,
            FieldInfo? loadOnGenerator,
            FieldInfo? maxAmpsConst)
        {
            AmpsNormalized = ampsNormalized;
            Amps = amps;
            MaxAmps = maxAmps;
            AmpsPerTm = ampsPerTm;
            MaxPerTm = maxPerTm;
            TotalAmps = totalAmps;
            TotalAmpsRef = totalAmpsRef;
            Working = working;
            LoadOnGenerator = loadOnGenerator;
            MaxAmpsConst = maxAmpsConst;
        }

        public FieldInfo? AmpsNormalized { get; }
        public FieldInfo? Amps { get; }
        public FieldInfo? MaxAmps { get; }
        public FieldInfo? AmpsPerTm { get; }
        public FieldInfo? MaxPerTm { get; }
        public FieldInfo? TotalAmps { get; }
        public FieldInfo? TotalAmpsRef { get; }
        public FieldInfo? Working { get; }
        public FieldInfo? LoadOnGenerator { get; }
        public FieldInfo? MaxAmpsConst { get; }

        public bool HasAny =>
            AmpsNormalized != null
            || Amps != null
            || MaxAmps != null
            || AmpsPerTm != null
            || MaxPerTm != null
            || TotalAmps != null
            || TotalAmpsRef != null
            || Working != null
            || LoadOnGenerator != null
            || MaxAmpsConst != null;
    }

    private static bool NameHas(string name, string token) =>
        name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

    private static float? SafePortValue(Port? port)
    {
        if (port == null)
        {
            return null;
        }

        try
        {
            return SafeFloat(port.Value);
        }
        catch
        {
            return null;
        }
    }

    private static float? SafePortReferenceValue(PortReference? pref)
    {
        if (pref == null || !pref.IsConnected)
        {
            return null;
        }

        try
        {
            return SafeFloat(pref.Value);
        }
        catch
        {
            return null;
        }
    }

    private static float? SafeFloat(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) ? null : value;

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
