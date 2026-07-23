using System;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using DV.Signs;
using DV.ThingTypes;
using LocoSim.Implementations;
using LocoSim.Resources;
using UnityEngine;
using YardMasterSuite.Core;
using Arc = BezierArcApproximation.Arc;
using Object = UnityEngine.Object;

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

    /// <summary>Per-track geometry speed limit (km/h), same ladder as SignPlacer / DVRouteManager.</summary>
    private static readonly Dictionary<int, float?> TrackSpeedLimitCache = new();

    private static readonly List<Arc> ArcScratch = new();

    /// <summary>Refresh loaded <see cref="SignDebug"/> boards periodically (streaming scenes).</summary>
    private const float SignDebugRefreshSeconds = 1.5f;

    /// <summary>How far behind the loco (m) to look for the governing posted board.</summary>
    private const float BoardLookbackMeters = 300f;

    /// <summary>Minimum lookahead (m) for the next posted board (**1.11**).</summary>
    private const float BoardLookaheadMinMeters = 500f;

    /// <summary>Lookahead scale: meters ≈ speed(km/h) × this.</summary>
    private const float BoardLookaheadSecondsOfSpeed = 6f;

    private static SignDebug[] _signDebugCache = Array.Empty<SignDebug>();
    private static float _signDebugCacheAt = -999f;

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

    /// <summary>
    /// Personal look heading (degrees 0–359, Unity +Z = north). Always available for the version-row chip.
    /// </summary>
    public static float? TryGetHeadingDegrees()
    {
        try
        {
            var cam = PlayerManager.ActiveCamera;
            if (cam != null)
            {
                var f = cam.transform.forward;
                return HeadingDisplay.FromForward(f.x, f.z);
            }

            var player = PlayerManager.PlayerTransform;
            if (player != null)
            {
                var f = player.forward;
                return HeadingDisplay.FromForward(f.x, f.z);
            }
        }
        catch
        {
            // fail closed
        }

        return null;
    }

    public static string CurrentHeadingLabel() =>
        HeadingDisplay.Format(TryGetHeadingDegrees());

    internal static HeadingDebugSnapshot CurrentHeadingDebugSnapshot() =>
        new(HeadingDisplay.ToCompassPoint(TryGetHeadingDegrees()));

    /// <summary>Player world position (XZ used by Marked / Station / AR / T2 pos).</summary>
    public static bool TryGetPlayerPosition(out float x, out float y, out float z)
    {
        x = y = z = 0f;
        try
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                return false;
            }

            var p = player.position;
            x = p.x;
            y = p.y;
            z = p.z;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static PositionDebugSnapshot CurrentPositionDebugSnapshot()
    {
        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return new PositionDebugSnapshot(null, null);
        }

        return new PositionDebugSnapshot(
            (int)System.Math.Round(x, System.MidpointRounding.AwayFromZero),
            (int)System.Math.Round(z, System.MidpointRounding.AwayFromZero));
    }

    /// <summary>Always-on park/return chip (1.14). Null when unmarked (omit from HUD).</summary>
    public static string? CurrentParkLabel()
    {
        if (!ParkMarkSession.TryGet(out var markX, out var markZ))
        {
            return ParkMarkDisplay.FormatReturn(null, null, null, null);
        }

        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return ParkMarkDisplay.FormatReturn(markX, markZ, null, null);
        }

        return ParkMarkDisplay.FormatReturn(markX, markZ, x, z);
    }

    internal static ParkDebugSnapshot CurrentParkDebugSnapshot()
    {
        if (!ParkMarkSession.TryGet(out var markX, out var markZ))
        {
            return new ParkDebugSnapshot(false, null);
        }

        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return new ParkDebugSnapshot(true, null);
        }

        return new ParkDebugSnapshot(true, ParkMarkDisplay.TryGetReturnPoint(markX, markZ, x, z));
    }

    /// <summary>Always-on in-zone station waypoint chip (4.6). Null outside zones.</summary>
    public static string? CurrentStationWaypointLabel()
    {
        if (!TryGetStationInPlayerZone(out var yardId, out var stationX, out var stationZ))
        {
            return StationWaypointDisplay.Format(
                inZone: false,
                yardId: null,
                stationX: null,
                stationZ: null,
                playerX: null,
                playerZ: null);
        }

        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return StationWaypointDisplay.Format(true, yardId, stationX, stationZ, null, null);
        }

        return StationWaypointDisplay.Format(true, yardId, stationX, stationZ, x, z);
    }

    internal static StationWaypointDebugSnapshot CurrentStationWaypointDebugSnapshot()
    {
        if (!TryGetStationInPlayerZone(out var yardId, out var stationX, out var stationZ))
        {
            return new StationWaypointDebugSnapshot(false, null, null);
        }

        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return new StationWaypointDebugSnapshot(true, yardId, null);
        }

        return new StationWaypointDebugSnapshot(
            true,
            yardId,
            StationWaypointDisplay.TryGetWalkPoint(stationX, stationZ, x, z));
    }

    /// <summary>
    /// Active Job HUD line (4.8), or null when no taken jobs (bar omitted).
    /// </summary>
    public static string? CurrentActiveJobHudLineOrNull()
    {
        if (!TryGetPrimaryActiveJob(out var job, out var extraCount) || job == null)
        {
            return null;
        }

        var remaining = BonusTimeDisplay.RemainingSeconds(job.TimeLimit, SafeTimeOnJob(job));
        var zoneMeters = TryGetActiveJobZoneMetersRemaining(job);
        return ActiveJobHudLine.Format(
            ActiveJobHudLine.FormatJobId(job.ID, extraCount),
            BonusTimeDisplay.Format(remaining, richText: true),
            ZoneEdgeDisplay.Format(zoneMeters, richText: true));
    }

    internal static ActiveJobDebugSnapshot CurrentActiveJobDebugSnapshot()
    {
        if (!TryGetPrimaryActiveJob(out var job, out _) || job == null)
        {
            return new ActiveJobDebugSnapshot(false, null, null, null);
        }

        var remaining = BonusTimeDisplay.RemainingSeconds(job.TimeLimit, SafeTimeOnJob(job));
        var zoneMeters = TryGetActiveJobZoneMetersRemaining(job);
        return new ActiveJobDebugSnapshot(
            true,
            job.ID,
            BonusTimeDisplay.Format(remaining, richText: false),
            ZoneEdgeDisplay.Format(zoneMeters, richText: false));
    }

    public static bool TrySetParkMarkAtPlayer()
    {
        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return false;
        }

        ParkMarkSession.Set(x, z);
        return true;
    }

    public static void ClearParkMark() => ParkMarkSession.Clear();

    /// <summary>Last/active loco world position for AR marker (4.9).</summary>
    public static bool TryGetArLocoWorldPosition(out Vector3 world)
    {
        world = default;
        try
        {
            var loco = PlayerManager.LastLoco;
            if (loco == null)
            {
                loco = TryGetUsableLoco();
            }

            if (loco == null)
            {
                return false;
            }

            world = loco.transform.position;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>In-zone station office world position (4.9 / fixed 4.6 target).</summary>
    public static bool TryGetArStationOfficeWorldPosition(out Vector3 world)
    {
        world = default;
        try
        {
            if (!TryGetStationControllerInPlayerZone(out var station) || station == null)
            {
                return false;
            }

            var range = station.GetComponent<StationJobGenerationRange>();
            if (range == null)
            {
                return false;
            }

            world = range.transform.position;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Custom pin from park mark session (4.9 / 1.14).</summary>
    public static bool TryGetArPinWorldPosition(out Vector3 world)
    {
        world = default;
        if (!ParkMarkSession.TryGet(out var x, out var z))
        {
            return false;
        }

        var y = 0f;
        try
        {
            if (PlayerManager.PlayerTransform != null)
            {
                y = PlayerManager.PlayerTransform.position.y;
            }
        }
        catch
        {
            // keep y = 0
        }

        world = new Vector3(x, y, z);
        return true;
    }

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

    /// <summary>
    /// Governing speed limit (km/h): last posted board behind the loco when available;
    /// otherwise current-track geometry (SignPlacer ladder).
    /// </summary>
    public static float? TryGetSpeedLimitKmh() => TryGetSpeedLimitState().CurrentKmh;

    /// <summary>↑/↓ vs next different posted board ahead (**1.11**).</summary>
    public static LimitTrend TryGetSpeedLimitTrend() => TryGetSpeedLimitState().Trend;

    private readonly struct SpeedLimitState
    {
        public SpeedLimitState(float? currentKmh, LimitTrend trend)
        {
            CurrentKmh = currentKmh;
            Trend = trend;
        }

        public float? CurrentKmh { get; }
        public LimitTrend Trend { get; }
    }

    private static SpeedLimitState TryGetSpeedLimitState()
    {
        try
        {
            var loco = TryGetUsableLoco();
            if (loco == null)
            {
                return new SpeedLimitState(null, LimitTrend.None);
            }

            var speedMps = loco.GetAbsSpeed();
            var speedKmh = SpeedDisplay.ToKilometersPerHour(speedMps);
            var boards = ScanPostedBoards(loco, speedKmh);
            var current = boards.CurrentKmh;
            if (current is null)
            {
                var bogie = loco.FrontBogie ?? loco.RearBogie;
                var track = bogie?.track;
                current = track == null ? null : GetOrComputeTrackSpeedLimitKmh(track);
            }

            var trend = SpeedLimitDisplay.TrendFrom(current, boards.NextKmh);
            return new SpeedLimitState(current, trend);
        }
        catch
        {
            return new SpeedLimitState(null, LimitTrend.None);
        }
    }

    private readonly struct PostedBoardScan
    {
        public PostedBoardScan(float? currentKmh, float? nextKmh)
        {
            CurrentKmh = currentKmh;
            NextKmh = nextKmh;
        }

        public float? CurrentKmh { get; }
        public float? NextKmh { get; }
    }

    /// <summary>
    /// Current = closest board behind; next = nearest different board ahead within lookahead.
    /// </summary>
    private static PostedBoardScan ScanPostedBoards(TrainCar loco, float speedKmh)
    {
        RefreshSignDebugCacheIfNeeded();
        if (_signDebugCache.Length == 0)
        {
            return new PostedBoardScan(null, null);
        }

        var pos = loco.transform.position;
        var fwd = TravelForward(loco);
        var lookahead = Mathf.Max(BoardLookaheadMinMeters, speedKmh * BoardLookaheadSecondsOfSpeed);
        var searchRadius = Mathf.Max(BoardLookbackMeters, lookahead);

        float? currentKmh = null;
        var bestBehindAlong = float.NegativeInfinity;
        float? nextKmh = null;
        var bestAheadAlong = float.PositiveInfinity;

        foreach (var sign in _signDebugCache)
        {
            if (sign == null)
            {
                continue;
            }

            var delta = sign.transform.position - pos;
            if (delta.sqrMagnitude > searchRadius * searchRadius)
            {
                continue;
            }

            var parsed = SpeedLimitBoardParser.ParseKmh(sign.text);
            if (parsed is null)
            {
                continue;
            }

            var along = Vector3.Dot(delta, fwd);
            if (along < 0f && along >= -BoardLookbackMeters && along > bestBehindAlong)
            {
                bestBehindAlong = along;
                currentKmh = parsed;
            }
            else if (along > 0f && along <= lookahead && along < bestAheadAlong)
            {
                bestAheadAlong = along;
                nextKmh = parsed;
            }
        }

        if (currentKmh is not null && nextKmh is not null
            && RoundLimit(currentKmh.Value) == RoundLimit(nextKmh.Value))
        {
            nextKmh = FindNextDifferentBoardAhead(pos, fwd, lookahead, currentKmh.Value);
        }

        return new PostedBoardScan(currentKmh, nextKmh);
    }

    private static float? FindNextDifferentBoardAhead(
        Vector3 pos,
        Vector3 fwd,
        float lookahead,
        float currentKmh)
    {
        var currentWhole = RoundLimit(currentKmh);
        float? best = null;
        var bestAlong = float.PositiveInfinity;
        foreach (var sign in _signDebugCache)
        {
            if (sign == null)
            {
                continue;
            }

            var parsed = SpeedLimitBoardParser.ParseKmh(sign.text);
            if (parsed is null || RoundLimit(parsed.Value) == currentWhole)
            {
                continue;
            }

            var along = Vector3.Dot(sign.transform.position - pos, fwd);
            if (along <= 0f || along > lookahead || along >= bestAlong)
            {
                continue;
            }

            bestAlong = along;
            best = parsed;
        }

        return best;
    }

    private static int RoundLimit(float kmh) =>
        (int)Math.Round(kmh, MidpointRounding.AwayFromZero);

    private static void RefreshSignDebugCacheIfNeeded()
    {
        if (Time.unscaledTime - _signDebugCacheAt < SignDebugRefreshSeconds)
        {
            return;
        }

        _signDebugCacheAt = Time.unscaledTime;
        try
        {
            _signDebugCache = Object.FindObjectsOfType<SignDebug>() ?? Array.Empty<SignDebug>();
        }
        catch
        {
            _signDebugCache = Array.Empty<SignDebug>();
        }
    }

    private static Vector3 TravelForward(TrainCar loco)
    {
        var fwd = loco.transform.forward;
        try
        {
            if (loco.GetForwardSpeed() < 0f)
            {
                fwd = -fwd;
            }
        }
        catch
        {
            // keep transform forward
        }

        return fwd;
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

        var fuel = TryGetFuelPercent();
        var oil = TryGetOilPercent();
        var speedMps = TryGetAbsSpeedMetersPerSecond();
        var speedKmh = speedMps is null
            ? (float?)null
            : SpeedDisplay.ToKilometersPerHour(speedMps.Value);
        var limit = TryGetSpeedLimitState();
        var nextStation = TryGetNextStationChip(fuel, oil);
        // 4.7 center-weighted IA: Fuel·Oil·Mass·Grade·Load·Speed·Limit·Motors·Handbrakes·Cars (+ optional Next)
        return TrainHudLine.Format(
            FluidDisplay.FormatFuelHud(fuel, oil),
            FluidDisplay.FormatOilHud(fuel, oil),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            LoadDisplay.FormatHud(TryGetLoadPercent()),
            SpeedDisplay.FormatFromMetersPerSecond(speedMps),
            SpeedLimitDisplay.FormatHud(speedKmh, limit.CurrentKmh, limit.Trend),
            MotorDisplay.FormatHud(TryGetMotorStatus()),
            HandbrakeDisplay.FormatTotal(TryGetConsistHandbrakeAppliedCount()),
            CarsDisplay.Format(TryGetConsistCarCount()),
            nextStation);
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

    /// <summary>Lead usable loco fuel container percent (null if unavailable).</summary>
    public static float? TryGetFuelPercent()
    {
        try
        {
            var flow = TryGetUsableLoco()?.SimController?.SimulationFlow;
            return flow == null ? null : ReadFluidPercent(flow, ResourceContainerType.FUEL);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Lead usable loco oil container percent (null if unavailable).</summary>
    public static float? TryGetOilPercent()
    {
        try
        {
            var flow = TryGetUsableLoco()?.SimController?.SimulationFlow;
            return flow == null ? null : ReadFluidPercent(flow, ResourceContainerType.OIL);
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
            TrackDisplay.Format(TryGetTrackId()),
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
            usable ? MotorDisplay.Format(TryGetMotorStatus()) : MotorDisplay.Format(null),
            usable ? FluidDisplay.FormatFuel(TryGetFuelPercent()) : FluidDisplay.FormatFuel(null),
            usable ? FluidDisplay.FormatOil(TryGetOilPercent()) : FluidDisplay.FormatOil(null));
    }

    internal static SpeedLimitDebugSnapshot CurrentSpeedLimitDebugSnapshot()
    {
        var usable = HasUsableLocoTrain();
        if (!usable)
        {
            return new SpeedLimitDebugSnapshot(
                false,
                SpeedDisplay.FormatFromMetersPerSecond(null),
                SpeedLimitDisplay.Format(null));
        }

        var limit = TryGetSpeedLimitState();
        return new SpeedLimitDebugSnapshot(
            true,
            SpeedDisplay.FormatFromMetersPerSecond(TryGetAbsSpeedMetersPerSecond()),
            SpeedLimitDisplay.Format(limit.CurrentKmh, limit.Trend));
    }

    private static float? GetOrComputeTrackSpeedLimitKmh(RailTrack track)
    {
        var id = track.GetInstanceID();
        if (TrackSpeedLimitCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var limit = ComputeTrackSpeedLimitKmh(track);
        TrackSpeedLimitCache[id] = limit;
        return limit;
    }

    /// <summary>
    /// Same approach as DVRouteManager: BezierArcApproximation min radius → SignPlacer table.
    /// </summary>
    private static float? ComputeTrackSpeedLimitKmh(RailTrack track)
    {
        var curve = track.curve;
        if (curve == null)
        {
            return null;
        }

        ArcScratch.Clear();
        BezierArcApproximation.CalculateArcs(curve, 0.5f, ArcScratch);
        if (ArcScratch.Count == 0)
        {
            return 120f;
        }

        var minRadius = float.PositiveInfinity;
        foreach (var arc in ArcScratch)
        {
            if (arc.r > 0f && arc.r < minRadius)
            {
                minRadius = arc.r;
            }
        }

        return SpeedLimitGeometry.MaxSpeedForMinRadius(minRadius);
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
            TrackDisplay.Format(TryGetTrackId()),
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
            job: "— Job",
            track: "— Track");

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

    /// <summary>Yard Track ID display string (e.g. SM-O6I), or null when unknown / generic mainline.</summary>
    private static string? TryGetTrackId()
    {
        try
        {
            var track = TryGetTargetCar()?.logicCar?.CurrentTrack;
            var id = track?.ID;
            if (id == null || id.IsGeneric())
            {
                return null;
            }

            var display = id.FullDisplayID?.Trim();
            return string.IsNullOrEmpty(display) ? null : display;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetNextStationChip(float? fuelPercent, float? oilPercent)
    {
        if (!NextStationDisplay.FluidsLow(fuelPercent, oilPercent))
        {
            return null;
        }

        if (!TryResolveNextStation(out var label, out var meters))
        {
            return null;
        }

        return NextStationDisplay.Format(true, label, meters);
    }

    internal static NextStationDebugSnapshot CurrentNextStationDebugSnapshot()
    {
        if (!HasUsableLocoTrain())
        {
            return new NextStationDebugSnapshot(false, null);
        }

        var chip = TryGetNextStationChip(TryGetFuelPercent(), TryGetOilPercent());
        return chip == null
            ? new NextStationDebugSnapshot(false, null)
            : new NextStationDebugSnapshot(true, chip);
    }

    private static bool TryGetStationInPlayerZone(
        out string? yardId,
        out float stationX,
        out float stationZ)
    {
        yardId = null;
        stationX = 0f;
        stationZ = 0f;
        try
        {
            if (!TryGetStationControllerInPlayerZone(out var station) || station == null)
            {
                return false;
            }

            var range = station.GetComponent<StationJobGenerationRange>();
            if (range == null)
            {
                return false;
            }

            // Office / booklet area is the range component's own transform.
            // stationCenterAnchor is the yard geometric center (wrong for foot nav to paperwork).
            var p = range.transform.position;
            stationX = p.x;
            stationZ = p.z;
            yardId = station.stationInfo?.YardID;
            if (string.IsNullOrWhiteSpace(yardId))
            {
                yardId = station.stationInfo?.Name;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetStationControllerInPlayerZone(out StationController? station)
    {
        station = null;
        try
        {
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return false;
            }

            StationController? best = null;
            var bestSqr = float.MaxValue;
            for (var i = 0; i < stations.Count; i++)
            {
                var candidate = stations[i];
                if (candidate == null || !candidate.StationInfoValid)
                {
                    continue;
                }

                var range = candidate.GetComponent<StationJobGenerationRange>();
                if (range == null)
                {
                    continue;
                }

                var sqr = range.PlayerSqrDistanceFromStationCenter;
                if (!range.IsPlayerInJobGenerationZone(sqr))
                {
                    continue;
                }

                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            station = best;
            return best != null;
        }
        catch
        {
            station = null;
            return false;
        }
    }

    private static bool TryResolveNextStation(out string? label, out float distanceMeters)
    {
        label = null;
        distanceMeters = 0f;
        try
        {
            if (!TryGetStartStationForNext(out var start) || start == null)
            {
                return false;
            }

            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return false;
            }

            StationController? bestDest = null;
            var bestDist = float.MaxValue;
            for (var i = 0; i < stations.Count; i++)
            {
                var dest = stations[i];
                if (dest == null || ReferenceEquals(dest, start) || !dest.StationInfoValid)
                {
                    continue;
                }

                var dist = JobPaymentCalculator.GetDistanceBetweenStations(start, dest);
                if (dist <= 0f || float.IsNaN(dist) || float.IsInfinity(dist))
                {
                    continue;
                }

                if (dist >= bestDist)
                {
                    continue;
                }

                bestDist = dist;
                bestDest = dest;
            }

            if (bestDest == null)
            {
                return false;
            }

            label = bestDest.stationInfo?.Name;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = bestDest.stationInfo?.YardID;
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            distanceMeters = bestDist;
            return true;
        }
        catch
        {
            label = null;
            distanceMeters = 0f;
            return false;
        }
    }

    private static bool TryGetStartStationForNext(out StationController? start)
    {
        start = null;
        if (TryGetStationControllerInPlayerZone(out start) && start != null)
        {
            return true;
        }

        try
        {
            var trackId = TryGetUsableLoco()?.logicCar?.CurrentTrack?.ID;
            if (trackId == null || trackId.IsGeneric())
            {
                return false;
            }

            var yard = trackId.yardId;
            if (string.IsNullOrWhiteSpace(yard))
            {
                return false;
            }

            start = StationController.GetStationByYardID(yard);
            return start != null && start.StationInfoValid;
        }
        catch
        {
            start = null;
            return false;
        }
    }

    private static bool TryGetPrimaryActiveJob(out Job? job, out int extraCount)
    {
        job = null;
        extraCount = 0;
        try
        {
            var jobs = JobsManager.Instance?.currentJobs;
            if (jobs == null || jobs.Count == 0)
            {
                return false;
            }

            Job? best = null;
            float? bestRemaining = null;
            for (var i = 0; i < jobs.Count; i++)
            {
                var candidate = jobs[i];
                if (candidate == null)
                {
                    continue;
                }

                var remaining = BonusTimeDisplay.RemainingSeconds(
                    candidate.TimeLimit,
                    SafeTimeOnJob(candidate));
                if (best == null)
                {
                    best = candidate;
                    bestRemaining = remaining;
                    continue;
                }

                // Prefer the job with the least bonus time remaining (most urgent).
                if (remaining is null)
                {
                    continue;
                }

                if (bestRemaining is null || remaining.Value < bestRemaining.Value)
                {
                    best = candidate;
                    bestRemaining = remaining;
                }
            }

            if (best == null)
            {
                return false;
            }

            job = best;
            extraCount = Math.Max(0, jobs.Count - 1);
            return true;
        }
        catch
        {
            job = null;
            extraCount = 0;
            return false;
        }
    }

    private static float? SafeTimeOnJob(Job job)
    {
        try
        {
            return job.GetTimeOnJob();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Meters remaining to the job-keep (destroy) zone edge for the job's origin yard,
    /// else the station the player is currently inside.
    /// </summary>
    private static float? TryGetActiveJobZoneMetersRemaining(Job job)
    {
        try
        {
            if (!TryResolveJobKeepStation(job, out var station) || station == null)
            {
                return null;
            }

            var range = station.GetComponent<StationJobGenerationRange>();
            if (range == null)
            {
                return null;
            }

            var radius = ZoneEdgeDisplay.RadiusFromSqr(range.destroyGeneratedJobsSqrDistanceAnyJobTaken);
            var playerDist = ZoneEdgeDisplay.DistanceFromSqr(range.PlayerSqrDistanceFromStationCenter);
            return ZoneEdgeDisplay.MetersRemaining(playerDist, radius);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryResolveJobKeepStation(Job job, out StationController? station)
    {
        station = null;
        try
        {
            var originYard = job.chainData?.chainOriginYardId;
            if (!string.IsNullOrWhiteSpace(originYard))
            {
                station = StationController.GetStationByYardID(originYard);
                if (station != null && station.StationInfoValid)
                {
                    return true;
                }
            }

            return TryGetStationControllerInPlayerZone(out station) && station != null;
        }
        catch
        {
            station = null;
            return false;
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

    /// <summary>
    /// Fuel/Oil (and other resources) share <see cref="ResourceContainer"/>;
    /// match <see cref="ResourceContainer.resourceType"/> then prefer normalized readout.
    /// </summary>
    private static float? ReadFluidPercent(SimulationFlow flow, ResourceContainerType resourceType)
    {
        if (flow?.OrderedSimComps == null)
        {
            return null;
        }

        foreach (var comp in flow.OrderedSimComps)
        {
            if (comp is not ResourceContainer container || container.resourceType != resourceType)
            {
                continue;
            }

            var normalized = SafePortValue(container.normalizedReadOutPort);
            if (normalized != null)
            {
                return FluidDisplay.PercentFromNormalized(normalized);
            }

            var fromAmount = FluidDisplay.PercentFromAmount(
                SafePortValue(container.amountReadOut),
                SafePortValue(container.capacityReadOutPort) ?? SafeFloat(container.capacity));
            if (fromAmount != null)
            {
                return fromAmount;
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
