using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV;
using DV.CabControls;
using DV.InventorySystem;
using DV.Logic.Job;
using DV.Signs;
using DV.Simulation.Cars;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
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

    /// <summary>FindObjectsOfType throttle for other-loco AR radar (4.10).</summary>
    private const float LocoRadarCacheSeconds = 2.5f;

    private static float _locoRadarCachedAt = -999f;
    private static readonly TrainCar?[] _locoRadarCars = new TrainCar?[LocoRadarSelection.DefaultMaxResults];
    private static readonly string?[] _locoRadarTypeIds = new string?[LocoRadarSelection.DefaultMaxResults];
    private static readonly string?[] _locoRadarPlaceLabels = new string?[LocoRadarSelection.DefaultMaxResults];
    private static int _locoRadarCount;

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

    /// <summary>Always-on in-zone station waypoint chip (4.6 / Bundle C). Null outside zones.</summary>
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
                playerZ: null,
                atOffice: false);
        }

        if (!TryGetPlayerPosition(out var x, out _, out var z))
        {
            return StationWaypointDisplay.Format(true, yardId, stationX, stationZ, null, null, atOffice: false);
        }

        var atOffice = IsPlayerAtOffice(x, z);
        return StationWaypointDisplay.Format(true, yardId, stationX, stationZ, x, z, atOffice);
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

        var atOffice = IsPlayerAtOffice(x, z);
        return new StationWaypointDebugSnapshot(
            true,
            yardId,
            StationWaypointDisplay.TryGetWalkPoint(stationX, stationZ, x, z, atOffice));
    }

    /// <summary>
    /// Active Job HUD (4.8 / license-warn):
    /// taken = Job+Bonus only; Cancelled flash; else license warn + optional Preview edge.
    /// Null when nothing to show.
    /// </summary>
    public static string? CurrentActiveJobHudLineOrNull()
    {
        EnsureJobLifecycleHooks();

        if (TryConsumeCancelledFlash(out var cancelledId))
        {
            return ActiveJobHudLine.FormatCancelled(cancelledId, richText: true);
        }

        if (TryGetPrimaryActiveJob(out var job, out var extraCount) && job != null)
        {
            if (ActiveJobHudLine.IsCancelledState(job.State.ToString()))
            {
                NoteCancelled(job.ID);
                return ActiveJobHudLine.FormatCancelled(job.ID, richText: true);
            }

            var remaining = BonusTimeDisplay.RemainingSeconds(job.TimeLimit, SafeTimeOnJob(job));
            return ActiveJobHudLine.Format(
                ActiveJobHudLine.FormatJobId(job.ID, extraCount),
                BonusTimeDisplay.Format(remaining, richText: true));
        }

        var licenseWarn = TryFormatHeldLicenseWarn(richText: true);
        string? previewChip = null;
        if (TryGetPreviewEdgeMetersRemaining(out var previewMeters))
        {
            previewChip = PreviewEdgeDisplay.Format(previewMeters, richText: true);
        }

        return ActiveJobHudLine.FormatPrep(licenseWarn, previewChip);
    }

    internal static ActiveJobDebugSnapshot CurrentActiveJobDebugSnapshot()
    {
        EnsureJobLifecycleHooks();

        if (TryConsumeCancelledFlash(out var cancelledId))
        {
            return new ActiveJobDebugSnapshot(true, cancelledId, "Cancelled", null);
        }

        if (TryGetPrimaryActiveJob(out var job, out _) && job != null)
        {
            if (ActiveJobHudLine.IsCancelledState(job.State.ToString()))
            {
                NoteCancelled(job.ID);
                return new ActiveJobDebugSnapshot(true, job.ID, "Cancelled", null);
            }

            var remaining = BonusTimeDisplay.RemainingSeconds(job.TimeLimit, SafeTimeOnJob(job));
            return new ActiveJobDebugSnapshot(
                true,
                job.ID,
                BonusTimeDisplay.Format(remaining, richText: false),
                null);
        }

        var licenseWarnPlain = TryFormatHeldLicenseWarn(richText: false);
        string? previewPlain = null;
        if (TryGetPreviewEdgeMetersRemaining(out var previewMeters))
        {
            previewPlain = PreviewEdgeDisplay.Format(previewMeters, richText: false);
        }

        if (licenseWarnPlain != null || previewPlain != null)
        {
            return new ActiveJobDebugSnapshot(true, null, null, previewPlain, licenseWarnPlain);
        }

        return new ActiveJobDebugSnapshot(false, null, null, null);
    }

    public static bool TrySetParkMarkAtPlayer()
    {
        if (!TryGetPlayerPosition(out var x, out var y, out var z))
        {
            return false;
        }

        ParkMarkSession.Set(x, y, z);
        return true;
    }

    public static void ClearParkMark() => ParkMarkSession.Clear();

    /// <summary>Last/active loco world position for AR marker (4.9). Hidden while player is in that loco (A.4).</summary>
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

            var playerCar = PlayerManager.Car;
            if (ArProximityHide.ShouldHideLocoMarker(playerCar != null && ReferenceEquals(playerCar, loco)))
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

    /// <summary>How many other-loco AR radar markers are available (4.10). Throttled scan.</summary>
    public static int GetArOtherLocoCount()
    {
        EnsureLocoRadarCache();
        return _locoRadarCount;
    }

    /// <summary>
    /// Nearest other loco for AR radar (4.10). Excludes self / my-loco AR target / same consist.
    /// Caption is live distance; world position comes from the cached car transform.
    /// </summary>
    public static bool TryGetArOtherLoco(int index, out Vector3 world, out string caption)
    {
        world = default;
        caption = "";
        try
        {
            EnsureLocoRadarCache();
            if (index < 0 || index >= _locoRadarCount)
            {
                return false;
            }

            var car = _locoRadarCars[index];
            if (car == null)
            {
                return false;
            }

            world = car.transform.position;
            var dist = 0f;
            if (TryGetPlayerPosition(out var px, out var py, out var pz))
            {
                var dx = world.x - px;
                var dy = world.y - py;
                var dz = world.z - pz;
                dist = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
            }

            caption = LocoRadarDisplay.FormatCaption(
                _locoRadarTypeIds[index],
                dist,
                _locoRadarPlaceLabels[index]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EnsureLocoRadarCache()
    {
        if (Time.unscaledTime - _locoRadarCachedAt < LocoRadarCacheSeconds)
        {
            return;
        }

        _locoRadarCachedAt = Time.unscaledTime;
        _locoRadarCount = 0;
        for (var i = 0; i < _locoRadarCars.Length; i++)
        {
            _locoRadarCars[i] = null;
            _locoRadarTypeIds[i] = null;
            _locoRadarPlaceLabels[i] = null;
        }

        if (!TryGetPlayerPosition(out var px, out var py, out var pz))
        {
            return;
        }

        TrainCar[] allCars;
        try
        {
            allCars = Object.FindObjectsOfType<TrainCar>() ?? Array.Empty<TrainCar>();
        }
        catch
        {
            return;
        }

        var exclude = new HashSet<int>();
        CollectLocoRadarExclusions(exclude);

        var candidates = new List<LocoRadarCandidate>(8);
        var byId = new Dictionary<int, TrainCar>(8);
        for (var i = 0; i < allCars.Length; i++)
        {
            var car = allCars[i];
            if (car == null || !car.IsLoco)
            {
                continue;
            }

            int id;
            try
            {
                id = car.GetInstanceID();
            }
            catch
            {
                continue;
            }

            if (exclude.Contains(id))
            {
                continue;
            }

            Vector3 pos;
            try
            {
                pos = car.transform.position;
            }
            catch
            {
                continue;
            }

            var dx = pos.x - px;
            var dy = pos.y - py;
            var dz = pos.z - pz;
            candidates.Add(new LocoRadarCandidate(id, (dx * dx) + (dy * dy) + (dz * dz)));
            byId[id] = car;
        }

        var ranked = new int[LocoRadarSelection.DefaultMaxResults];
        var n = LocoRadarSelection.RankNearest(
            candidates,
            excludeIds: null,
            LocoRadarSelection.DefaultMaxResults,
            ranked);
        for (var i = 0; i < n; i++)
        {
            if (!byId.TryGetValue(ranked[i], out var car) || car == null)
            {
                continue;
            }

            _locoRadarCars[_locoRadarCount] = car;
            _locoRadarTypeIds[_locoRadarCount] = TryGetLocoTypeId(car);
            _locoRadarPlaceLabels[_locoRadarCount] = TryGetLocoRadarPlaceLabel(car);
            _locoRadarCount++;
        }
    }

    private static void CollectLocoRadarExclusions(HashSet<int> exclude)
    {
        void AddCar(TrainCar? car)
        {
            if (car == null)
            {
                return;
            }

            try
            {
                exclude.Add(car.GetInstanceID());
            }
            catch
            {
                // ignored
            }
        }

        void AddTrainsetLocos(TrainCar? seed)
        {
            if (seed == null)
            {
                return;
            }

            AddCar(seed);
            try
            {
                var cars = seed.trainset?.cars;
                if (cars == null)
                {
                    return;
                }

                for (var i = 0; i < cars.Count; i++)
                {
                    var c = cars[i];
                    if (c != null && c.IsLoco)
                    {
                        AddCar(c);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        AddTrainsetLocos(PlayerManager.Car);
        AddCar(PlayerManager.LastLoco);
        AddTrainsetLocos(TryGetUsableLoco());
    }

    private static string? TryGetLocoTypeId(TrainCar car)
    {
        try
        {
            if (!car.IsLoco)
            {
                return null;
            }

            return car.carLivery?.parentType?.id ?? car.carLivery?.id;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Place for radar: identifiable <c>SM-T12P</c>-style track alone; otherwise city + track (e.g. <c>FF #Y</c>).
    /// Spur tracks may be <c>IsGeneric()</c> but still expose FullDisplayID / yardId <c>#Y</c> — keep that token.
    /// </summary>
    private static string? TryGetLocoRadarPlaceLabel(TrainCar car)
    {
        try
        {
            string? trackDisplay = null;
            string? trackYard = null;
            var track = car.logicCar?.CurrentTrack;
            var id = track?.ID;
            if (id != null)
            {
                // Prefer FullDisplayID even when IsGeneric — #Y spurs are often generic but labeled.
                var display = id.FullDisplayID?.Trim();
                if (!string.IsNullOrEmpty(display))
                {
                    trackDisplay = display;
                }

                trackYard = id.yardId?.Trim();
                // If display was blank, keep spur junk (#Y) as the track token (not as city).
                if (string.IsNullOrEmpty(trackDisplay)
                    && !string.IsNullOrEmpty(trackYard)
                    && !LocoRadarDisplay.IsUsableCityYardId(trackYard))
                {
                    trackDisplay = trackYard;
                }
            }

            string? city;
            if (LocoRadarDisplay.TrackIncludesCity(trackDisplay))
            {
                city = null;
            }
            else
            {
                var nearest = TryGetNearestYardId(car.transform.position);
                city = LocoRadarDisplay.IsUsableCityYardId(nearest) ? nearest : null;
                if (city == null && LocoRadarDisplay.IsUsableCityYardId(trackYard))
                {
                    city = trackYard;
                }
            }

            return LocoRadarDisplay.FormatPlace(trackDisplay, city);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Closest station YardID (or Name) to world XZ — no job-zone gate.</summary>
    private static string? TryGetNearestYardId(Vector3 world)
    {
        try
        {
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return null;
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
                var center = range != null
                    ? range.transform.position
                    : candidate.transform.position;
                var dx = world.x - center.x;
                var dz = world.z - center.z;
                var sqr = (dx * dx) + (dz * dz);
                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            if (best == null)
            {
                return null;
            }

            var yardId = best.stationInfo?.YardID?.Trim();
            if (!string.IsNullOrEmpty(yardId))
            {
                return yardId;
            }

            var name = best.stationInfo?.Name?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// In-zone station office world position (4.9 / Bundle C).
    /// Hidden while <see cref="IsPlayerAtOffice"/> — same gate as Station chip <c>here</c>.
    /// </summary>
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

            var office = range.transform.position;
            if (TryGetPlayerPosition(out var px, out _, out var pz) && IsPlayerAtOffice(station, office, px, pz))
            {
                return false;
            }

            world = office;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Bundle C: one predicate for house AR hide and Station <c>here</c>.
    /// Exact building AABB when available; else flat <see cref="ArProximityHide.OfficeHideRadiusMeters"/>.
    /// </summary>
    private static bool IsPlayerAtOffice(float playerX, float playerZ)
    {
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

            return IsPlayerAtOffice(station, range.transform.position, playerX, playerZ);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPlayerAtOffice(StationController station, Vector3 office, float playerX, float playerZ)
    {
        if (StationOfficeBounds.TryGetHideAabb(station, office, out var aabb))
        {
            return ArProximityHide.IsAtOffice(aabb, playerX, playerZ);
        }

        return ArProximityHide.IsAtOffice(office.x, office.z, playerX, playerZ);
    }

    /// <summary>Custom pin from park mark session (4.9 / 1.14). Uses Y stored at mark time.</summary>
    public static bool TryGetArPinWorldPosition(out Vector3 world)
    {
        world = default;
        if (!ParkMarkSession.TryGet(out var x, out var y, out var z))
        {
            return false;
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
            var car = TryGetTargetCar();
            return CouplerDebugOverride.ApplyFront(car?.ID, TryGetLinkStatus(car?.frontCoupler));
        }
        catch
        {
            return CouplerDebugOverride.ApplyFront(TryGetTargetCar()?.ID, null);
        }
    }

    public static CouplerLinkStatus? TryGetRearLinkStatus()
    {
        try
        {
            var car = TryGetTargetCar();
            return CouplerDebugOverride.ApplyRear(car?.ID, TryGetLinkStatus(car?.rearCoupler));
        }
        catch
        {
            return CouplerDebugOverride.ApplyRear(TryGetTargetCar()?.ID, null);
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
        // 4.7 center-weighted IA: Fuel·Oil·Mass·Grade·Load·Speed·Limit·Motors·Handbrakes·Cars
        // 4.5 Next: station — cut (nearest-yard chip was clutter / wrong for mainland range).
        return TrainHudLine.Format(
            FluidDisplay.FormatFuelHud(fuel, oil),
            FluidDisplay.FormatOilHud(fuel, oil),
            TonnageDisplay.FormatFromKilograms(TryGetConsistMassKilograms()),
            GradeDisplay.FormatPercent(TryGetGradePercent()),
            LoadDisplay.FormatHud(TryGetLoadPercent()),
            SpeedDisplay.FormatFromMetersPerSecond(speedMps),
            SpeedLimitDisplay.FormatHud(speedKmh, limit.CurrentKmh, limit.Trend),
            FormatMotorsHudChip(),
            HandbrakeDisplay.FormatTotal(TryGetConsistHandbrakeAppliedCount()),
            CarsDisplay.Format(TryGetConsistCarCount()),
            TryGetBackupProximityHudChip());
    }

    /// <summary>Motors chip with debug heat % and governor flash when capping.</summary>
    private static string FormatMotorsHudChip()
    {
        var locoId = TryGetUsableLoco()?.ID;
        var flashOn = ((int)(Time.unscaledTime * 4f) & 1) == 0;
        return MotorDisplay.FormatHud(
            TryGetMotorStatus(),
            governorActive: ThermalGovernor.IsCapping,
            flashOn: flashOn,
            forcedHeatPercent: MotorDebugOverride.ForcedHeatPercent(locoId));
    }

    /// <summary>
    /// 4.11 — free consist extremity clearance / couple-ready cue (empty = omit).
    /// </summary>
    public static string TryGetBackupProximityHudChip()
    {
        try
        {
            if (!TryGetBackupProximity(out var meters, out var inRange, out var tipActive))
            {
                return string.Empty;
            }

            return BackupProximityDisplay.FormatHud(meters, inRange, tipActive);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Overlap buffer for 4.11 clearance (game couple-scan uses only 10 — too small at 80 m).</summary>
    private static readonly Collider[] BackupProximityHits = new Collider[128];

    /// <summary>
    /// Free tip on loco rear axis: clearance via scan / cone; green when within 1.5 m game scan
    /// (still <c>Rear Nm</c> — not "Couple ready"). Tip with no range → Rear —.
    /// </summary>
    public static bool TryGetBackupProximity(
        out float? clearanceMeters,
        out bool inCoupleRange,
        out bool tipActive)
    {
        clearanceMeters = null;
        inCoupleRange = false;
        tipActive = false;

        var loco = TryGetUsableLoco();
        if (loco == null)
        {
            return false;
        }

        var coupler = TryGetApproachTipCoupler(loco);
        if (coupler == null || coupler.IsCoupled())
        {
            return false;
        }

        tipActive = true;

        // Game API only at couple range (1.5 m). Distance uses DV check points (not pivot).
        var near = coupler.GetFirstCouplerInRange(Coupler.COUPLING_SCAN_RANGE);
        if (near != null)
        {
            clearanceMeters = Vector3.Distance(CouplerClearancePoint(coupler), CouplerClearancePoint(near));
            inCoupleRange = true;
            return true;
        }

        if (TryScanNearestForeignCoupler(coupler, out var hitMeters))
        {
            clearanceMeters = hitMeters;
            inCoupleRange = BackupProximityDisplay.IsInCoupleRange(hitMeters);
            return true;
        }

        return true;
    }

    /// <summary>
    /// Matches private <c>Coupler.CouplerDistanceCheckPosition</c>: inset 0.25 m along −forward
    /// so clearance tracks couple/scan geometry, not the coupler pivot (false 0.0 with open buffers).
    /// </summary>
    private static Vector3 CouplerClearancePoint(Coupler coupler)
    {
        var t = coupler.transform;
        var fwd = t.forward;
        if (fwd.sqrMagnitude < 1e-6f)
        {
            return t.position;
        }

        return t.position + fwd.normalized * -0.25f;
    }

    /// <summary>
    /// Free extremity tip on the loco rear axis (−forward). Cab look is ignored (rear-camera chip).
    /// </summary>
    private static Coupler? TryGetApproachTipCoupler(TrainCar loco)
    {
        var set = loco.trainset?.cars;
        if (set == null || set.Count == 0)
        {
            return null;
        }

        var locoFwd = loco.transform.forward;
        BackupProximityAim.RearIntent(
            locoFwd.x,
            locoFwd.y,
            locoFwd.z,
            out var ix,
            out var iy,
            out var iz);

        Coupler? best = null;
        var bestAlign = float.NegativeInfinity;

        foreach (var c in set)
        {
            if (c == null)
            {
                continue;
            }

            Consider(c.frontCoupler);
            Consider(c.rearCoupler);
        }

        return best;

        void Consider(Coupler? coupler)
        {
            if (coupler == null || coupler.IsCoupled())
            {
                return;
            }

            var opposite = coupler.GetOppositeCoupler();
            var isAlone = set!.Count == 1;
            var oppositeCoupled = opposite != null && opposite.IsCoupled();
            if (!isAlone && !oppositeCoupled)
            {
                return;
            }

            var o = coupler.transform.forward;
            var align = BackupProximityAim.TipAlignment(o.x, o.y, o.z, ix, iy, iz);
            if (align > bestAlign)
            {
                bestAlign = align;
                best = coupler;
            }
        }
    }

    /// <summary>
    /// Nearest uncoupled foreign coupler in the tip's outward approach cone (own large buffer).
    /// Does not use exact CurrentTrack equality (Bezier segments break that).
    /// </summary>
    private static bool TryScanNearestForeignCoupler(Coupler tip, out float meters)
    {
        meters = 0f;
        var origin = CouplerClearancePoint(tip);
        var tipOut = tip.transform.forward;
        if (tipOut.sqrMagnitude < 1e-6f)
        {
            return false;
        }

        tipOut.Normalize();
        const float maxDistance = BackupProximityDisplay.MaxDisplayMeters;
        var maxSq = maxDistance * maxDistance;

        int numHits;
        try
        {
            numHits = Physics.OverlapSphereNonAlloc(
                origin,
                maxDistance,
                BackupProximityHits,
                ~0,
                QueryTriggerInteraction.Ignore);
        }
        catch
        {
            return false;
        }

        var ownSet = tip.train?.trainset;
        var nearestSq = maxSq;
        var found = false;

        for (var i = 0; i < numHits; i++)
        {
            var col = BackupProximityHits[i];
            if (col == null)
            {
                continue;
            }

            var hitCar = TrainCar.Resolve(col.transform);
            if (hitCar == null || (ownSet != null && hitCar.trainset == ownSet))
            {
                continue;
            }

            ConsiderCoupler(hitCar.frontCoupler);
            ConsiderCoupler(hitCar.rearCoupler);
        }

        // RaycastAll along tip outward: skip own consist, take nearest foreign car hit.
        try
        {
            var rayOrigin = tip.transform.position + Vector3.up * 0.35f;
            var rayHits = Physics.RaycastAll(rayOrigin, tipOut, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in rayHits)
            {
                var hitCar = hit.collider != null ? TrainCar.Resolve(hit.collider.transform) : null;
                if (hitCar == null || (ownSet != null && hitCar.trainset == ownSet))
                {
                    continue;
                }

                var dSq = hit.distance * hit.distance;
                if (dSq < nearestSq)
                {
                    nearestSq = dSq;
                    found = true;
                }
            }
        }
        catch
        {
            // keep overlap result
        }

        if (!found)
        {
            return false;
        }

        meters = Mathf.Sqrt(nearestSq);
        return true;

        void ConsiderCoupler(Coupler? other)
        {
            if (other == null || other.IsCoupled())
            {
                return;
            }

            var otherPt = CouplerClearancePoint(other);
            var delta = otherPt - origin;
            var distSq = delta.sqrMagnitude;
            if (distSq > maxSq || distSq < 1e-6f)
            {
                return;
            }

            if (!BackupProximityAim.IsInApproachCone(
                    delta.x, delta.y, delta.z, tipOut.x, tipOut.y, tipOut.z))
            {
                return;
            }

            if (distSq < nearestSq)
            {
                nearestSq = distSq;
                found = true;
            }
        }
    }

    /// <summary>Legacy join helper — empty when top bar is hidden.</summary>
    public static string CurrentTrainHudLine() =>
        CurrentTrainHudLineOrNull() ?? string.Empty;

    /// <summary>Lead usable loco traction load as percent of max amps (null if unavailable).</summary>
    public static float? TryGetLoadPercent()
    {
        try
        {
            var loco = TryGetUsableLoco();
            var flow = loco?.SimController?.SimulationFlow;
            var real = flow == null ? null : ReadLoadPercent(flow);
            return LoadDebugOverride.Apply(loco?.ID, real);
        }
        catch
        {
            return LoadDebugOverride.Apply(TryGetUsableLoco()?.ID, null);
        }
    }

    /// <summary>Lead usable loco TM cab status (null if unavailable).</summary>
    public static MotorStatus? TryGetMotorStatus()
    {
        try
        {
            var loco = TryGetUsableLoco();
            var flow = loco?.SimController?.SimulationFlow;
            var real = flow == null ? null : ReadMotorStatus(flow, TryGetCabTempBand(loco));
            return MotorDebugOverride.ApplyStatus(loco?.ID, real);
        }
        catch
        {
            return MotorDebugOverride.ApplyStatus(TryGetUsableLoco()?.ID, null);
        }
    }

    /// <summary>Lead usable loco fuel container percent (null if unavailable).</summary>
    public static float? TryGetFuelPercent()
    {
        try
        {
            var loco = TryGetUsableLoco();
            var flow = loco?.SimController?.SimulationFlow;
            var real = flow == null ? null : ReadFluidPercent(flow, ResourceContainerType.FUEL);
            return FluidDebugOverride.ApplyFuel(loco?.ID, real);
        }
        catch
        {
            return FluidDebugOverride.ApplyFuel(TryGetUsableLoco()?.ID, null);
        }
    }

    /// <summary>Lead usable loco oil container percent (null if unavailable).</summary>
    public static float? TryGetOilPercent()
    {
        try
        {
            var loco = TryGetUsableLoco();
            var flow = loco?.SimController?.SimulationFlow;
            var real = flow == null ? null : ReadFluidPercent(flow, ResourceContainerType.OIL);
            return FluidDebugOverride.ApplyOil(loco?.ID, real);
        }
        catch
        {
            return FluidDebugOverride.ApplyOil(TryGetUsableLoco()?.ID, null);
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
            TryGetLocoTypeLabel(car),
            TryGetCarMassLabel(car));
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
            TryGetLocoTypeLabel(car),
            TryGetCarMassLabel(car));
    }

    private static LocalCarDebugSnapshot HiddenLocalCarSnapshot() =>
        new(
            visible: false,
            pipe: "— Pipe",
            handbrake: "— Handbrake",
            coupling: "— Couplers",
            carNumber: CarNumberDisplay.NotOnTrainLabel,
            job: null,
            track: null);

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

    /// <summary>Single-car mass (+ Consist total when coupled) for look-at / standing bar.</summary>
    private static string? TryGetCarMassLabel(TrainCar car)
    {
        try
        {
            if (car.massController == null)
            {
                return null;
            }

            var carKg = car.massController.TotalMass;
            if (carKg <= 0f)
            {
                return null;
            }

            return TonnageDisplay.FormatCarAndConsistFromKilograms(
                carKg,
                TryGetTrainsetMassKilograms(car));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Sum of all cars in this car's trainset (coupled consist), fail-closed.</summary>
    private static float? TryGetTrainsetMassKilograms(TrainCar car)
    {
        try
        {
            var set = car.trainset;
            var cars = set?.cars;
            if (cars == null || cars.Count == 0)
            {
                return car.massController != null ? car.massController.TotalMass : null;
            }

            float total = 0f;
            var any = false;
            foreach (var c in cars)
            {
                if (c?.massController == null)
                {
                    continue;
                }

                total += c.massController.TotalMass;
                any = true;
            }

            return any ? total : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Tier 2 F7: unload ↔ full load for <b>all freight</b> in the look-at / standing car's
    /// trainset (coupled consist). Uses game CargoLoaded/Unloaded events. Fail-closed.
    /// </summary>
    public static bool TryDebugCycleTargetCargo(out string message)
    {
        message = "no freight";
        try
        {
            var seed = TryGetTargetCar();
            if (seed == null)
            {
                return false;
            }

            var freight = CollectTrainsetFreight(seed);
            if (freight.Count == 0)
            {
                return false;
            }

            var anyHasCargo = false;
            foreach (var car in freight)
            {
                if (FreightHasCargo(car))
                {
                    anyHasCargo = true;
                    break;
                }
            }

            var action = CargoDebugCycle.NextAction(anyHasCargo);
            var ok = 0;
            var fail = 0;

            if (action == CargoDebugAction.Unload)
            {
                foreach (var car in freight)
                {
                    var logic = car.logicCar;
                    if (logic == null || !FreightHasCargo(car))
                    {
                        continue;
                    }

                    try
                    {
                        logic.UnloadCargo(logic.LoadedCargoAmount, logic.CurrentCargoTypeInCar);
                        ok++;
                    }
                    catch
                    {
                        fail++;
                    }
                }

                message = fail == 0
                    ? $"unloaded {ok}/{freight.Count} freight"
                    : $"unloaded {ok}/{freight.Count} freight ({fail} fail)";
                return ok > 0;
            }

            foreach (var car in freight)
            {
                var logic = car.logicCar;
                if (logic == null)
                {
                    fail++;
                    continue;
                }

                if (TryLoadFullOnLogic(car, logic, out _, out _))
                {
                    ok++;
                }
                else
                {
                    fail++;
                }
            }

            message = fail == 0
                ? $"loaded {ok}/{freight.Count} freight"
                : $"loaded {ok}/{freight.Count} freight ({fail} fail)";
            return ok > 0;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    private static bool FreightHasCargo(TrainCar car)
    {
        var logic = car.logicCar;
        return logic != null
            && logic.CurrentCargoTypeInCar != CargoType.None
            && logic.LoadedCargoAmount > 0f;
    }

    /// <summary>Non-loco cars in <paramref name="seed"/>'s trainset (or just seed if alone).</summary>
    private static List<TrainCar> CollectTrainsetFreight(TrainCar seed)
    {
        var list = new List<TrainCar>();
        try
        {
            var cars = seed.trainset?.cars;
            if (cars == null || cars.Count == 0)
            {
                if (!seed.IsLoco)
                {
                    list.Add(seed);
                }

                return list;
            }

            foreach (var c in cars)
            {
                if (c != null && !c.IsLoco)
                {
                    list.Add(c);
                }
            }
        }
        catch
        {
            if (!seed.IsLoco)
            {
                list.Add(seed);
            }
        }

        return list;
    }

    /// <summary>
    /// Full load using last-unloaded type when possible, else first loadable type for this car.
    /// Uses <see cref="Car.LoadCargo"/> so TrainCar events refresh mass and cargo model.
    /// </summary>
    private static bool TryLoadFullOnLogic(TrainCar car, Car logic, out CargoType loaded, out string error)
    {
        loaded = CargoType.None;
        error = "load failed";

        var capacity = car.cargoCapacity > 0f ? car.cargoCapacity : logic.capacity;
        if (capacity <= 0f)
        {
            error = "no capacity";
            return false;
        }

        if (logic.CurrentCargoTypeInCar != CargoType.None)
        {
            logic.UnloadCargo(logic.LoadedCargoAmount, logic.CurrentCargoTypeInCar);
        }

        foreach (var cargo in EnumerateLoadCandidates(car, logic))
        {
            try
            {
                logic.LoadCargo(capacity, cargo, null);
                loaded = cargo;
                return true;
            }
            catch
            {
                // try next candidate
            }
        }

        error = "no loadable cargo";
        return false;
    }

    private static IEnumerable<CargoType> EnumerateLoadCandidates(TrainCar car, Car logic)
    {
        var list = new List<CargoType>();
        if (logic.LastUnloadedCargoType != CargoType.None)
        {
            list.Add(logic.LastUnloadedCargoType);
        }

        try
        {
            var parent = car.carLivery?.parentType;
            if (parent != null &&
                Globals.G?.Types?.CarTypeToLoadableCargo != null &&
                Globals.G.Types.CarTypeToLoadableCargo.TryGetValue(parent, out var loadable) &&
                loadable != null)
            {
                foreach (var c in loadable)
                {
                    if (c?.v1 is { } t && t != CargoType.None && !list.Contains(t))
                    {
                        list.Add(t);
                    }
                }
            }
        }
        catch
        {
            // Globals / livery lookup fail-closed → fall through to hard-coded list
        }

        foreach (var fallback in new[]
                 {
                     CargoType.IronOre,
                     CargoType.Coal,
                     CargoType.SteelRails,
                     CargoType.SteelSlabs,
                     CargoType.ScrapMetal,
                 })
        {
            if (!list.Contains(fallback))
            {
                list.Add(fallback);
            }
        }

        return list;
    }

    private static LicenseDebugMode _licenseDebugMode = LicenseDebugMode.Real;
    private static List<GeneralLicenseType_v2>? _licenseSnapshotGeneral;
    private static List<JobLicenseType_v2>? _licenseSnapshotJob;

    /// <summary>
    /// Tier 2 F11: grant <b>all</b> obtainable general + job licenses, then restore the
    /// pre-override snapshot on the next press ("real"). Fail-closed.
    /// </summary>
    public static bool TryDebugToggleAllLicenses(out string message)
    {
        message = "fail";
        try
        {
            var lm = LicenseManager.Instance;
            if (lm == null)
            {
                message = "no LicenseManager";
                return false;
            }

            var next = LicenseDebugToggle.Next(_licenseDebugMode);
            if (next == LicenseDebugMode.AllGranted)
            {
                if (!TrySnapshotLicenses(lm, out message))
                {
                    return false;
                }

                if (!TryAcquireAllLicenses(lm, out message))
                {
                    _licenseSnapshotGeneral = null;
                    _licenseSnapshotJob = null;
                    return false;
                }

                _licenseDebugMode = LicenseDebugMode.AllGranted;
                message = LicenseDebugToggle.StatusFragment(_licenseDebugMode);
                return true;
            }

            if (!TryRestoreLicenseSnapshot(lm, out message))
            {
                return false;
            }

            _licenseDebugMode = LicenseDebugMode.Real;
            _licenseSnapshotGeneral = null;
            _licenseSnapshotJob = null;
            message = LicenseDebugToggle.StatusFragment(_licenseDebugMode);
            return true;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    /// <summary>Restore real licenses if F11 override is active (e.g. debug gate off).</summary>
    public static void RestoreLicenseDebugIfNeeded()
    {
        if (_licenseDebugMode != LicenseDebugMode.AllGranted)
        {
            return;
        }

        TryDebugToggleAllLicenses(out _);
    }

    private static bool TrySnapshotLicenses(LicenseManager lm, out string message)
    {
        message = "snapshot fail";
        try
        {
            var general = lm.GetGeneralAcquiredLicenses();
            var jobs = lm.GetAcquiredJobLicenses();
            _licenseSnapshotGeneral = general != null
                ? new List<GeneralLicenseType_v2>(general)
                : new List<GeneralLicenseType_v2>();
            _licenseSnapshotJob = jobs != null
                ? new List<JobLicenseType_v2>(jobs)
                : new List<JobLicenseType_v2>();
            return true;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    private static bool TryAcquireAllLicenses(LicenseManager lm, out string message)
    {
        message = "acquire fail";
        var acquired = 0;
        try
        {
            foreach (GeneralLicenseType t in Enum.GetValues(typeof(GeneralLicenseType)))
            {
                if (t == GeneralLicenseType.NotSet)
                {
                    continue;
                }

                var v2 = TransitionHelpers.ToV2(t);
                if (v2 == null)
                {
                    continue;
                }

                try
                {
                    if (!lm.IsGeneralLicenseObtainable(v2) && !lm.IsGeneralLicenseAcquired(v2))
                    {
                        continue;
                    }

                    if (!lm.IsGeneralLicenseAcquired(v2))
                    {
                        lm.AcquireGeneralLicense(v2);
                        acquired++;
                    }
                }
                catch
                {
                    // skip unobtainable / blocked
                }
            }

            foreach (JobLicenses t in Enum.GetValues(typeof(JobLicenses)))
            {
                var v2 = TransitionHelpers.ToV2(t);
                if (v2 == null)
                {
                    continue;
                }

                try
                {
                    if (!lm.IsJobLicenseObtainable(v2) && !lm.IsJobLicenseAcquired(v2))
                    {
                        continue;
                    }

                    if (!lm.IsJobLicenseAcquired(v2))
                    {
                        lm.AcquireJobLicense(v2);
                        acquired++;
                    }
                }
                catch
                {
                    // skip unobtainable / blocked
                }
            }

            message = $"acquired +{acquired}";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    private static bool TryRestoreLicenseSnapshot(LicenseManager lm, out string message)
    {
        message = "restore fail";
        try
        {
            var keepGeneral = new HashSet<GeneralLicenseType_v2>(
                _licenseSnapshotGeneral ?? Enumerable.Empty<GeneralLicenseType_v2>());
            var keepJob = new HashSet<JobLicenseType_v2>(
                _licenseSnapshotJob ?? Enumerable.Empty<JobLicenseType_v2>());

            var currentGeneral = lm.GetGeneralAcquiredLicenses();
            if (currentGeneral != null)
            {
                foreach (var lic in currentGeneral.ToArray())
                {
                    if (lic != null && !keepGeneral.Contains(lic))
                    {
                        lm.RemoveGeneralLicense(lic);
                    }
                }
            }

            var currentJob = lm.GetAcquiredJobLicenses();
            if (currentJob != null)
            {
                var toRemove = currentJob.Where(j => j != null && !keepJob.Contains(j)).ToList();
                if (toRemove.Count > 0)
                {
                    lm.RemoveJobLicense(toRemove);
                }
            }

            foreach (var lic in keepGeneral)
            {
                if (lic != null && !lm.IsGeneralLicenseAcquired(lic))
                {
                    lm.AcquireGeneralLicense(lic);
                }
            }

            foreach (var lic in keepJob)
            {
                if (lic != null && !lm.IsJobLicenseAcquired(lic))
                {
                    lm.AcquireJobLicense(lic);
                }
            }

            message = "restored";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    private static LighterDebugPhase _lighterDebugPhase = LighterDebugPhase.Real;
    private static GameObject? _debugLighterGo;

    /// <summary>
    /// Tier 2 F5: cycle lighter inventory — give → remove (lost&amp;found) → real. Avoids F12 console.
    /// </summary>
    public static bool TryDebugCycleLighter(out string message)
    {
        message = "fail";
        try
        {
            var next = LighterDebugCycle.Next(_lighterDebugPhase);
            switch (next)
            {
                case LighterDebugPhase.InInventory:
                    if (!TryGiveDebugLighter(out message))
                    {
                        return false;
                    }

                    _lighterDebugPhase = LighterDebugPhase.InInventory;
                    return true;

                case LighterDebugPhase.Removed:
                    TryRemoveDebugLighter(out message);
                    _lighterDebugPhase = LighterDebugPhase.Removed;
                    return true;

                default:
                    TryRemoveDebugLighter(out _);
                    _lighterDebugPhase = LighterDebugPhase.Real;
                    message = "lighter real";
                    return true;
            }
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    private static bool TryGiveDebugLighter(out string message)
    {
        message = "no inventory";
        var inv = Inventory.Instance;
        if (inv == null)
        {
            return false;
        }

        var existing = inv.GetItemByName("lighter", partialNameCheck: true, includeDropped: false);
        if (existing != null)
        {
            _debugLighterGo = existing;
            message = "lighter already present";
            return true;
        }

        var prefab = Resources.Load<GameObject>("lighter");
        if (prefab == null)
        {
            message = "no lighter prefab";
            return false;
        }

        var go = Object.Instantiate(prefab);
        go.name = "lighter";
        var spec = go.GetComponent<InventoryItemSpec>();
        if (spec != null)
        {
            spec.BelongsToPlayer = true;
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (!inv.CanAddItem(go))
        {
            Object.Destroy(go);
            message = "inventory full";
            return false;
        }

        var slot = inv.AddItemToInventory(go);
        if (slot < 0)
        {
            Object.Destroy(go);
            message = "add failed";
            return false;
        }

        _debugLighterGo = go;
        message = "lighter given";
        return true;
    }

    private static void TryRemoveDebugLighter(out string message)
    {
        message = "no lighter";
        try
        {
            var inv = Inventory.Instance;
            var go = _debugLighterGo;
            if ((go == null || go.Equals(null)) && inv != null)
            {
                go = inv.GetItemByName("lighter", partialNameCheck: true, includeDropped: true);
            }

            if (go == null || go.Equals(null))
            {
                _debugLighterGo = null;
                message = "lighter already gone";
                return;
            }

            // Unequip first — DestroyItem while held NRE's in Lighter.OnDestroy.
            if (inv != null)
            {
                try
                {
                    inv.UnequipItem(true, -1);
                }
                catch
                {
                    // best-effort
                }

                try
                {
                    inv.DropItemFromHandsOrInventory(go);
                }
                catch
                {
                    // continue to lost&found
                }
            }

            var storage = StorageController.Instance;
            if (storage != null)
            {
                var item = go.GetComponent<ItemBase>() ?? go.GetComponentInChildren<ItemBase>();
                if (item != null)
                {
                    storage.AddItemToLostAndFound(item, true);
                    message = "lighter → Lost&Found";
                }
                else if (inv != null)
                {
                    inv.DropItemFromHandsOrInventory(go);
                    message = "lighter dropped (no ItemBase)";
                }
                else
                {
                    message = "no ItemBase";
                }
            }
            else if (inv != null)
            {
                // Last resort: drop only (do not DestroyItem — leaves ghost UI + NRE).
                inv.DropItemFromHandsOrInventory(go);
                message = "lighter dropped";
            }
            else
            {
                message = "no storage";
            }

            _debugLighterGo = null;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            _debugLighterGo = null;
        }
    }

    /// <summary>
    /// Turntable debug input: simulates bar/lever push via the same °/s FixedUpdate uses.
    /// Hold = full rate (12°/s). Tap assist = bar-like rate (≈2.4°/s) only within 2 m of lock.
    /// </summary>
    private static float _turntableHoldPushSign;
    private static bool _turntableSnapAssist;
    private static bool _turntableSnapAssistLoggedStart;

    /// <summary>Hold PageUp/Down: simulate full push (±1 → 12°/s).</summary>
    public static void SetTurntableHoldPush(float directionSign)
    {
        _turntableSnapAssist = false;
        _turntableHoldPushSign = directionSign == 0f ? 0f : (directionSign > 0f ? 1f : -1f);
    }

    public static void ClearTurntableHoldPush() => _turntableHoldPushSign = 0f;

    /// <summary>
    /// Tap: begin bar-push assist toward nearest track if within 2 m arc of lock.
    /// </summary>
    public static bool TryBeginTurntableSnapAssist(out string message)
    {
        message = "no turntable";
        _turntableHoldPushSign = 0f;
        _turntableSnapAssist = false;
        _turntableSnapAssistLoggedStart = false;
        try
        {
            if (!TryGetEligibleTurntable(out var ctrl, out _) || ctrl == null)
            {
                return false;
            }

            var track = ctrl.turntable;
            if (track == null)
            {
                message = "no track";
                return false;
            }

            var snap = track.ClosestSnappingAngle();
            if (float.IsNaN(snap) || snap < 0f)
            {
                message = "no snap angle";
                return false;
            }

            var delta = TurntableRailTrack.AngleRangeNeg180To180(snap - track.currentYRotation);
            var halfLen = track.SearchRadius;
            if (!TurntableSnapRange.IsWithinLockArc(delta, halfLen))
            {
                var arc = TurntableSnapRange.ArcMeters(delta, halfLen);
                message = $"out of snap range ({arc:0.0} m > {TurntableSnapRange.MaxLockArcMeters:0} m)";
                return false;
            }

            _turntableSnapAssist = true;
            message = $"snap assist ({TurntableSnapRange.ArcMeters(delta, halfLen):0.0} m to lock)";
            return true;
        }
        catch (Exception ex)
        {
            message = ex.GetType().Name;
            return false;
        }
    }

    public static void CancelTurntableSnapAssist() => _turntableSnapAssist = false;

    /// <summary>
    /// Call from <c>FixedUpdate</c>: inject bar/lever-equivalent rotation (no SetAngle teleport).
    /// </summary>
    public static void ApplyTurntableBarSimulation(float fixedDeltaTime, out string? status)
    {
        status = null;
        try
        {
            if (_turntableHoldPushSign == 0f && !_turntableSnapAssist)
            {
                return;
            }

            if (!TryGetEligibleTurntable(out var ctrl, out _) || ctrl == null)
            {
                _turntableSnapAssist = false;
                _turntableHoldPushSign = 0f;
                status = "lost turntable";
                return;
            }

            var track = ctrl.turntable;
            if (track == null)
            {
                return;
            }

            ctrl.PlayerControlAllowed = true;

            // DV: push field ~0.2 → intensity 0.2 * 12°/s ≈ 2.4°/s (bar).
            // Full lever intensity 1 → 12°/s (MAX_ROTATION_SPEED_DEGREES_PER_SEC).
            const float maxDegPerSec = 12f;
            const float barIntensity = 0.2f;

            float sign;
            float intensity;
            if (_turntableSnapAssist)
            {
                var snap = track.ClosestSnappingAngle();
                if (float.IsNaN(snap) || snap < 0f)
                {
                    _turntableSnapAssist = false;
                    status = "snap assist abort";
                    return;
                }

                var delta = TurntableRailTrack.AngleRangeNeg180To180(snap - track.currentYRotation);
                var halfLen = track.SearchRadius;
                if (!TurntableSnapRange.IsWithinLockArc(delta, halfLen))
                {
                    _turntableSnapAssist = false;
                    status = "left snap range";
                    return;
                }

                if (Math.Abs(delta) < 0.5f)
                {
                    track.targetYRotation = TurntableRailTrack.AngleRange0To360(snap);
                    track.RotateToTargetRotation(true);
                    _turntableSnapAssist = false;
                    status = $"locked {snap:0.0}";
                    return;
                }

                sign = Math.Sign(delta);
                intensity = barIntensity;
                if (!_turntableSnapAssistLoggedStart)
                {
                    _turntableSnapAssistLoggedStart = true;
                    status = $"bar-push → {snap:0.0}";
                }
            }
            else
            {
                sign = _turntableHoldPushSign;
                intensity = 1f;
            }

            var step = sign * intensity * maxDegPerSec * fixedDeltaTime;
            if (Math.Abs(step) < 0.00001f)
            {
                return;
            }

            var next = TurntableRailTrack.AngleRange0To360(track.currentYRotation + step);
            track.targetYRotation = next;
            track.RotateToTargetRotation(false);
        }
        catch
        {
            _turntableSnapAssist = false;
            _turntableHoldPushSign = 0f;
        }
    }

    private static bool TryGetEligibleTurntable(out TurntableController? ctrl, out float distance) =>
        TryGetNearbyTurntable(out ctrl, out distance);

    private static bool TryGetNearbyTurntable(out TurntableController? ctrl, out float distance)
    {
        ctrl = null;
        distance = float.MaxValue;
        try
        {
            // Prefer turntable under the crosshair (cab or yard walk).
            if (TryGetLookAtTurntable(out var lookAt) && lookAt != null)
            {
                ctrl = lookAt;
                var player = PlayerManager.PlayerTransform;
                distance = player != null
                    ? Vector3.Distance(player.position, lookAt.transform.position)
                    : 0f;
                return true;
            }

            var playerTf = PlayerManager.PlayerTransform;
            if (playerTf == null)
            {
                return false;
            }

            var pos = playerTf.position;
            ctrl = TurntableController.FindClosestTo(pos);
            if (ctrl == null)
            {
                return false;
            }

            distance = Vector3.Distance(pos, ctrl.transform.position);
            // Prefer: turntable SearchRadius (bridge half-length) + 15 m — cab or walk.
            var search = ctrl.turntable != null ? ctrl.turntable.SearchRadius : 40f;
            var max = search + 15f;
            if (distance > max)
            {
                ctrl = null;
                return false;
            }

            return true;
        }
        catch
        {
            ctrl = null;
            return false;
        }
    }

    private static bool TryGetLookAtTurntable(out TurntableController? ctrl)
    {
        ctrl = null;
        try
        {
            var cam = PlayerManager.PlayerCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                return false;
            }

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, 250f))
            {
                return false;
            }

            ctrl = hit.collider.GetComponentInParent<TurntableController>();
            if (ctrl != null)
            {
                return true;
            }

            var rail = hit.collider.GetComponentInParent<TurntableRailTrack>();
            if (rail == null)
            {
                return false;
            }

            ctrl = rail.GetComponentInParent<TurntableController>()
                ?? rail.GetComponent<TurntableController>()
                ?? TurntableController.FindClosestTo(rail.transform.position);
            return ctrl != null;
        }
        catch
        {
            ctrl = null;
            return false;
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

    private const float CancelledFlashSeconds = 8f;
    private static float _cancelledUntil = -1f;
    private static string? _cancelledJobId;
    private static Job? _lifecycleHookJob;

    private static void EnsureJobLifecycleHooks()
    {
        try
        {
            Job? target = null;
            if (TryGetPrimaryActiveJob(out var job, out _) && job != null)
            {
                target = job;
            }

            if (ReferenceEquals(_lifecycleHookJob, target))
            {
                return;
            }

            UnhookJobLifecycle();
            if (target == null)
            {
                return;
            }

            target.JobAbandoned += OnJobCancelled;
            target.JobExpired += OnJobCancelled;
            target.JobCompleted += OnJobCompleted;
            _lifecycleHookJob = target;
        }
        catch
        {
            // fail closed — Cancelled flash optional
        }
    }

    private static void UnhookJobLifecycle()
    {
        if (_lifecycleHookJob == null)
        {
            return;
        }

        try
        {
            _lifecycleHookJob.JobAbandoned -= OnJobCancelled;
            _lifecycleHookJob.JobExpired -= OnJobCancelled;
            _lifecycleHookJob.JobCompleted -= OnJobCompleted;
        }
        catch
        {
            // ignore
        }

        _lifecycleHookJob = null;
    }

    private static void OnJobCancelled(Job job) =>
        NoteCancelled(job != null ? job.ID : null);

    private static void OnJobCompleted(Job _)
    {
        _cancelledUntil = -1f;
        _cancelledJobId = null;
    }

    private static void NoteCancelled(string? jobId)
    {
        _cancelledJobId = jobId;
        _cancelledUntil = Time.unscaledTime + CancelledFlashSeconds;
    }

    private static bool TryConsumeCancelledFlash(out string? jobId)
    {
        jobId = null;
        if (_cancelledUntil < 0f || Time.unscaledTime > _cancelledUntil)
        {
            _cancelledUntil = -1f;
            _cancelledJobId = null;
            return false;
        }

        // Prefer live taken job over cancelled flash.
        if (TryGetPrimaryActiveJob(out var live, out _) && live != null
            && !ActiveJobHudLine.IsCancelledState(live.State.ToString()))
        {
            _cancelledUntil = -1f;
            _cancelledJobId = null;
            return false;
        }

        jobId = _cancelledJobId;
        return true;
    }

    /// <summary>
    /// Bundle D primary story: meters to <c>destroyGeneratedJobsSqrDistanceRegular</c>
    /// while holding any job paperwork in inventory (overview and/or booklet — multi-job prep).
    /// Gate: currentJobs empty + inventory has ≥1 job item. Does not use board availableJobs alone
    /// (Preview only when you have tickets on you). Station = most urgent origin among held jobs.
    /// </summary>
    private static bool TryGetPreviewEdgeMetersRemaining(out float? metersRemaining)
    {
        metersRemaining = null;
        try
        {
            var current = JobsManager.Instance?.currentJobs;
            if (current != null && current.Count > 0)
            {
                return false;
            }

            if (!TryGetJobsFromPlayerInventory(out var heldJobs) || heldJobs.Count == 0)
            {
                return false;
            }

            float? best = null;
            for (var i = 0; i < heldJobs.Count; i++)
            {
                if (!TryResolvePreviewStationForJob(heldJobs[i], out var station) || station == null)
                {
                    continue;
                }

                var range = station.GetComponent<StationJobGenerationRange>();
                if (range == null)
                {
                    continue;
                }

                var radius = PreviewEdgeDisplay.RadiusFromSqr(range.destroyGeneratedJobsSqrDistanceRegular);
                var playerDist = PreviewEdgeDisplay.DistanceFromSqr(range.PlayerSqrDistanceFromStationCenter);
                var remaining = PreviewEdgeDisplay.MetersRemaining(playerDist, radius);
                if (remaining is null)
                {
                    continue;
                }

                // Most urgent wipe among multi-job inventory.
                if (best is null || remaining.Value < best.Value)
                {
                    best = remaining;
                }
            }

            metersRemaining = best;
            return metersRemaining != null;
        }
        catch
        {
            metersRemaining = null;
            return false;
        }
    }

    /// <summary>
    /// Any <see cref="JobOverview"/> or <see cref="JobBooklet"/> in backpack/hotbar/hands
    /// (including dropped-but-still-tracked slots when <c>includingDropped</c> is true).
    /// </summary>
    private static bool TryGetJobsFromPlayerInventory(out List<Job> jobs)
    {
        var found = new List<Job>();
        jobs = found;
        try
        {
            var inv = Inventory.Instance;
            if (inv == null)
            {
                return false;
            }

            var seen = new HashSet<Job>();
            void Consider(GameObject? go)
            {
                if (go == null)
                {
                    return;
                }

                Job? job = null;
                var overview = go.GetComponent<JobOverview>();
                if (overview != null)
                {
                    job = overview.job;
                }
                else
                {
                    var booklet = go.GetComponent<JobBooklet>();
                    if (booklet != null)
                    {
                        job = booklet.job;
                    }
                }

                if (job == null || !seen.Add(job))
                {
                    return;
                }

                found.Add(job);
            }

            var items = inv.GetItemsArray(includingDropped: true);
            if (items != null)
            {
                for (var i = 0; i < items.Length; i++)
                {
                    Consider(items[i]);
                }
            }

            // Hands / equip slots (multi-job prep often holds a ticket).
            var handCap = inv.HandCapacity;
            for (var h = 0; h < handCap; h++)
            {
                Consider(inv.GetEquippedItemAtSlot(h));
            }

            return found.Count > 0;
        }
        catch
        {
            jobs = new List<Job>();
            return false;
        }
    }

    /// <summary>
    /// Missing job licenses for held overviews/booklets (pre-validate). Fail-closed → null.
    /// </summary>
    private static string? TryFormatHeldLicenseWarn(bool richText)
    {
        try
        {
            if (!TryGetMissingLicenseCodesForHeldJobs(out var codes) || codes.Count == 0)
            {
                return null;
            }

            return LicenseWarnDisplay.Format(codes, richText);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetMissingLicenseCodesForHeldJobs(out List<string> codes)
    {
        codes = new List<string>();
        try
        {
            if (!TryGetJobsFromPlayerInventory(out var jobs) || jobs.Count == 0)
            {
                return false;
            }

            var lm = LicenseManager.Instance;
            if (lm == null)
            {
                return false;
            }

            var raw = new List<string>();
            foreach (var job in jobs)
            {
                if (job == null)
                {
                    continue;
                }

                var required = JobLicenseType_v2.ToV2List(job.requiredLicenses);
                if (required == null || required.Count == 0)
                {
                    continue;
                }

                if (lm.IsLicensedForJob(required))
                {
                    continue;
                }

                var missing = lm.GetMissingLicensesForJob(required);
                if (missing == null || missing.Count == 0)
                {
                    continue;
                }

                foreach (var lic in missing)
                {
                    if (lic == null)
                    {
                        continue;
                    }

                    raw.Add(lic.v1.ToString());
                }
            }

            var normalized = LicenseWarnDisplay.NormalizeCodes(raw);
            if (normalized.Count == 0)
            {
                return false;
            }

            codes.AddRange(normalized);
            return true;
        }
        catch
        {
            codes = new List<string>();
            return false;
        }
    }

    private static bool TryResolvePreviewStationForJob(Job job, out StationController? station)
    {
        station = null;
        try
        {
            var originYard = job.chainData?.chainOriginYardId;
            if (!string.IsNullOrWhiteSpace(originYard))
            {
                station = StationController.GetStationByYardID(originYard);
                if (station != null && station.StationInfoValid
                    && station.GetComponent<StationJobGenerationRange>() != null)
                {
                    return true;
                }
            }

            // Fallback: nearest station with a generation-range component (no gen-zone gate).
            return TryGetNearestStationWithJobRange(out station);
        }
        catch
        {
            station = null;
            return false;
        }
    }

    private static bool TryGetNearestStationWithJobRange(out StationController? station)
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

    /// <summary>Lead usable loco for <see cref="ThermalGovernor"/> (same as HUD power path).</summary>
    internal static TrainCar? TryGetUsableLocoForGovernor() => TryGetUsableLoco();

    /// <summary>Cab MU temp band for thermal soft-cap ceilings (Warning vs Critical).</summary>
    internal static MotorCabTempBand? TryGetCabTempBandForGovernor()
    {
        var loco = TryGetUsableLoco();
        return MotorDebugOverride.ApplyBand(loco?.ID, TryGetCabTempBand(loco));
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

    private static MotorStatus? ReadMotorStatus(SimulationFlow flow, MotorCabTempBand? cabTempBand = null)
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

            var fromComp = ReadMotorStatusFromComponent(comp, cabTempBand);
            if (fromComp != null)
            {
                return fromComp;
            }
        }

        return null;
    }

    /// <summary>
    /// Cab TM TEMP lamp band from <see cref="MultipleUnitStateObserver.MUChainTemperatureState"/>
    /// (Warning = yellow — below TM critical overheating threshold).
    /// </summary>
    private static MotorCabTempBand? TryGetCabTempBand(TrainCar? loco)
    {
        if (loco == null)
        {
            return null;
        }

        try
        {
            var mu = loco.GetComponent<MultipleUnitStateObserver>();
            if (mu == null)
            {
                return null;
            }

            return (MotorCabTempBand)(int)mu.MUChainTemperatureState;
        }
        catch
        {
            return null;
        }
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

    private static MotorStatus? ReadMotorStatusFromComponent(SimComponent comp, MotorCabTempBand? cabTempBand = null)
    {
        if (comp is TractionMotor tm)
        {
            return MotorDisplay.StatusFromSignals(
                SafePortValue(tm.tmsStateReadOut),
                SafePortReferenceValue(tm.temperature),
                SafeFloat(tm.overheatingTemperatureThreshold),
                SafePortValue(tm.workingTractionMotorsReadOut),
                tm.numberOfTractionMotors,
                cabTempBand);
        }

        if (comp is TractionMotorSet set)
        {
            return ReadMotorStatusFromMotorSet(set, cabTempBand);
        }

        return null;
    }

    private static MotorStatus? ReadMotorStatusFromMotorSet(TractionMotorSet set, MotorCabTempBand? cabTempBand = null)
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
            ReadIntAsFloatField(set, map.Value.NumberOfMotors),
            cabTempBand);
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
        var cockOpenThisEnd = coupler.IsCockOpen;
        var cocksOpen = AreCocksOpenBothSides(coupler);
        // Loco↔loco MU: read this end's muModule even when not mechanically coupled (MU-only).
        TryGetMuCableState(coupler, other, out var muPresent, out var muConnected);
        return CouplingLink.Resolve(
            mechanicallyCoupled,
            tightened,
            airHoseConnected,
            cocksOpen,
            cockOpenThisEnd,
            muPresent,
            muConnected);
    }

    /// <summary>
    /// MU cable required when both cars are MU-capable, or when this end's MU is already plugged.
    /// Connection from <see cref="TrainCar.muModule"/> without needing a mechanical couple first.
    /// </summary>
    private static void TryGetMuCableState(
        Coupler coupler,
        Coupler? other,
        out bool muPresent,
        out bool muConnected)
    {
        muPresent = false;
        muConnected = false;

        var car = coupler.train;
        if (car == null || !car.IsMultipleUnit)
        {
            return;
        }

        var mod = car.muModule;
        if (mod != null)
        {
            muConnected = coupler.isFrontCoupler ? mod.ConnectedFront : mod.ConnectedRear;
        }
        else
        {
            var mu = TryGetMuAdapter(coupler);
            if (mu != null && mu.IsInitialized)
            {
                muConnected = mu.IsConnected;
            }
        }

        var otherCar = other?.train;
        if (otherCar != null && otherCar.IsMultipleUnit)
        {
            muPresent = true;
        }
        else if (muConnected)
        {
            // MU plugged with no mechanical couple yet — still a mid-couple / MU context.
            muPresent = true;
        }
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
