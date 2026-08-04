using System;
using System.Collections.Generic;
using DV;
using DV.ThingTypes;
using DV.Utils;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// 3.1b license-gated loco spawn: look-at place + native CarDestinationHighlighter + CarSpawner.
/// </summary>
internal static class LocoSpawnGovernor
{
    private const float MaxAimRayMeters = 250f;
    private const float MaxTrackSnapMeters = 12f;

    private static bool _busy;
    private static List<TrainCarLivery>? _cached;
    private static CarDestinationHighlighter? _ghost;
    private static GameObject? _ghostGo;
    private static GameObject? _ghostArrowGo;
    private static Material? _validMat;
    private static Material? _invalidMat;
    private static bool _ghostMissLogged;

    public static string ToggleMode()
    {
        if (LocoSpawnPlaceSession.IsActive)
        {
            return Cancel();
        }

        if (JobCarsPlaceSession.IsActive)
        {
            return "T2 loco-spawn: blocked (job place active)";
        }

        var list = RefreshLicensedLiveries();
        if (list.Count == 0)
        {
            return "T2 loco-spawn: no licensed locos";
        }

        LocoSpawnPlaceSession.Begin(0);
        EnsureGhost();
        var label = LocoSpawnPolicy.ShortLiveryLabel(list[0].id);
        var line = $"T2 loco-spawn: on · {list.Count} licensed · {label}";
        Main.Log(line);
        return line;
    }

    public static string Cancel()
    {
        TurnOffGhost();
        LocoSpawnPlaceSession.Clear();
        _cached = null;
        Main.Log("T2 loco-spawn: cancelled");
        return "loco-spawn cancelled";
    }

    public static void Scroll(int delta)
    {
        if (!LocoSpawnPlaceSession.IsActive)
        {
            return;
        }

        var list = RefreshLicensedLiveries();
        if (list.Count == 0)
        {
            Cancel();
            return;
        }

        var next = LocoSpawnPolicy.StepIndex(list.Count, LocoSpawnPlaceSession.SelectedIndex, delta);
        LocoSpawnPlaceSession.SetSelectedIndex(next);
        var label = LocoSpawnPolicy.ShortLiveryLabel(list[next].id);
        Main.Log($"T2 loco-spawn: select {label} ({next + 1}/{list.Count})");
    }

    public static void FlipFacing()
    {
        if (!LocoSpawnPlaceSession.IsActive)
        {
            return;
        }

        LocoSpawnPlaceSession.ToggleFacing();
        Main.Log(LocoSpawnPlaceSession.ForceRegularDirection
            ? "T2 loco-spawn: facing regular"
            : "T2 loco-spawn: facing flipped");
    }

    public static string CurrentChip()
    {
        if (!LocoSpawnPlaceSession.IsActive)
        {
            return "";
        }

        var list = _cached ?? RefreshLicensedLiveries();
        string? label = null;
        if (list.Count > 0)
        {
            var idx = LocoSpawnPolicy.WrapIndex(list.Count, LocoSpawnPlaceSession.SelectedIndex);
            label = LocoSpawnPolicy.ShortLiveryLabel(list[idx].id);
        }

        var abort = LocoSpawnPolicy.Evaluate(
            list.Count,
            !string.IsNullOrEmpty(LocoSpawnPlaceSession.TargetTrackId),
            LocoSpawnPlaceSession.HasAimPoint && !LocoSpawnPlaceSession.PlaceOk,
            _busy);

        if (list.Count > 0
            && !string.IsNullOrEmpty(LocoSpawnPlaceSession.TargetTrackId)
            && LocoSpawnPlaceSession.HasAimPoint
            && !LocoSpawnPlaceSession.PlaceOk
            && !_busy)
        {
            abort = LocoSpawnAbort.Unsafe;
        }

        return LocoSpawnPolicy.FormatPlaceChip(
            true,
            label,
            LocoSpawnPlaceSession.TargetTrackId,
            abort);
    }

    public static void PollAimAndGhost()
    {
        if (!LocoSpawnPlaceSession.IsActive)
        {
            return;
        }

        var list = RefreshLicensedLiveries();
        if (list.Count == 0)
        {
            Cancel();
            return;
        }

        var idx = LocoSpawnPolicy.WrapIndex(list.Count, LocoSpawnPlaceSession.SelectedIndex);
        LocoSpawnPlaceSession.SetSelectedIndex(idx);
        var livery = list[idx];
        var half = ClampLocoHalfExtents(GetLiveryHalfExtents(livery));

        if (!TryResolveLookPoint(out var look))
        {
            LocoSpawnPlaceSession.ClearAim();
            TurnOffGhost();
            return;
        }

        var tracks = RailTrackRegistry.RailTracks;
        if (tracks == null || tracks.Length == 0)
        {
            LocoSpawnPlaceSession.ClearAim();
            TurnOffGhost();
            return;
        }

        // Prefer a free rail slot near look-at (same family as native crew/re-rail).
        var available = CarSpawner.GetPointOnClosestAvailableTrackForCar(
            look,
            half,
            tracks,
            startRange: 0f,
            rangeIncrement: 2f,
            maxRange: 20f);

        RailTrack track;
        Vector3 aim;
        Vector3 forward;
        bool placeOk;

        if (available.HasValue)
        {
            var pair = available.Value;
            track = pair.Item1;
            var pt = pair.Item2;
            aim = (Vector3)pt.position;
            forward = pt.forward;
            placeOk = true;
        }
        else if (TrySnapToClosestTrack(look, tracks, out track, out aim, out forward))
        {
            // Snapped for feedback, but no free car-sized slot nearby.
            placeOk = false;
        }
        else
        {
            LocoSpawnPlaceSession.ClearAim();
            TurnOffGhost();
            return;
        }

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        if (!LocoSpawnPlaceSession.ForceRegularDirection)
        {
            forward = -forward;
        }

        var trackKey = PathGraphBuilder.TrackKey(track);
        LocoSpawnPlaceSession.SetTargetTrack(trackKey);
        LocoSpawnPlaceSession.SetAimPoint(aim.x, aim.y, aim.z);
        LocoSpawnPlaceSession.SetPlaceOk(placeOk);
        UpdateGhost(aim, forward, half, placeOk);
    }

    public static string ConfirmSpawn()
    {
        if (!LocoSpawnPlaceSession.IsActive)
        {
            return "T2 loco-spawn: inactive";
        }

        if (_busy)
        {
            return "T2 loco-spawn: busy";
        }

        var list = RefreshLicensedLiveries();
        if (list.Count == 0)
        {
            return "T2 loco-spawn: no licensed locos";
        }

        var idx = LocoSpawnPolicy.WrapIndex(list.Count, LocoSpawnPlaceSession.SelectedIndex);
        var livery = list[idx];
        var lm = LicenseManager.Instance;
        if (lm == null || !IsLicenseOk(lm, livery))
        {
            return "T2 loco-spawn: not licensed";
        }

        if (!LocoSpawnPlaceSession.TryGetAimPoint(out var ax, out var ay, out var az)
            || string.IsNullOrEmpty(LocoSpawnPlaceSession.TargetTrackId))
        {
            return "T2 loco-spawn: no track target";
        }

        if (!LocoSpawnPlaceSession.PlaceOk)
        {
            return "T2 loco-spawn: no space";
        }

        var spawner = SingletonBehaviour<CarSpawner>.Instance;
        if (spawner == null)
        {
            return "T2 loco-spawn: no CarSpawner";
        }

        _busy = true;
        try
        {
            var pos = new Vector3(ax, ay, az);
            var flip = !LocoSpawnPlaceSession.ForceRegularDirection;
            var car = spawner.SpawnCarOnClosestTrack(pos, livery, flip, playerSpawnedCar: true, uniqueCar: false);
            if (car == null)
            {
                return "T2 loco-spawn: spawn failed";
            }

            var label = LocoSpawnPolicy.ShortLiveryLabel(livery.id);
            var line = $"T2 loco-spawn: spawned {label} · {car.ID}";
            Main.Log(line);
            Cancel();
            return line;
        }
        catch (Exception ex)
        {
            Main.Log("T2 loco-spawn: exception " + ex.GetType().Name);
            return "T2 loco-spawn: " + ex.GetType().Name;
        }
        finally
        {
            _busy = false;
        }
    }

    private static List<TrainCarLivery> RefreshLicensedLiveries()
    {
        var result = new List<TrainCarLivery>();
        try
        {
            var lm = LicenseManager.Instance;
            var types = Globals.G?.Types;
            var liveries = types?.Liveries;
            if (lm == null || liveries == null)
            {
                _cached = result;
                return result;
            }

            foreach (var livery in liveries)
            {
                if (livery == null || livery.isHidden || livery.prefab == null)
                {
                    continue;
                }

                if (!LocoSpawnPolicy.IsEligibleSpawnLocoId(livery.id)
                    && !LocoSpawnPolicy.IsEligibleSpawnLocoId(livery.parentType?.id))
                {
                    continue;
                }

                // Must look like a loco kind / id.
                if (!IsLocoLivery(livery))
                {
                    continue;
                }

                if (!IsLicenseOk(lm, livery))
                {
                    continue;
                }

                result.Add(livery);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        }
        catch
        {
            // fail-closed empty
        }

        _cached = result;
        return result;
    }

    private static bool IsLicenseOk(LicenseManager lm, TrainCarLivery livery)
    {
        try
        {
            // Game's career gate. Slug/Relic already excluded by id.
            return lm.IsLicensedForCar(livery);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLocoLivery(TrainCarLivery livery)
    {
        if (LocoSpawnPolicy.IsEligibleSpawnLocoId(livery.id)
            || LocoSpawnPolicy.IsEligibleSpawnLocoId(livery.parentType?.id))
        {
            return true;
        }

        var kindId = livery.parentType?.kind?.id;
        return !string.IsNullOrEmpty(kindId)
            && kindId!.Equals("Loco", StringComparison.OrdinalIgnoreCase)
            && LocoSpawnPolicy.IsEligibleSpawnLocoId(livery.id ?? "Loco");
    }

    private static Vector3 GetLiveryHalfExtents(TrainCarLivery livery)
    {
        try
        {
            if (livery.prefab != null)
            {
                var col = livery.prefab.GetComponentInChildren<Collider>();
                if (col != null)
                {
                    var size = col.bounds.size;
                    return new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
                }
            }
        }
        catch
        {
            // fall through
        }

        return new Vector3(1.5f, 2f, 7f);
    }

    /// <summary>Prefab world bounds are often huge; keep a loco-sized box for overlap + ghost.</summary>
    private static Vector3 ClampLocoHalfExtents(Vector3 half)
    {
        return new Vector3(
            Mathf.Clamp(half.x, 0.8f, 2.2f),
            Mathf.Clamp(half.y, 1.2f, 3.5f),
            Mathf.Clamp(half.z, 4f, 10f));
    }

    private static bool TryResolveLookPoint(out Vector3 look)
    {
        look = default;
        try
        {
            var cam = PlayerManager.PlayerCamera ?? Camera.main;
            if (cam == null)
            {
                return false;
            }

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, MaxAimRayMeters))
            {
                look = hit.point;
            }
            else
            {
                look = ray.GetPoint(Mathf.Min(40f, MaxAimRayMeters));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySnapToClosestTrack(
        Vector3 look,
        RailTrack[] tracks,
        out RailTrack track,
        out Vector3 aim,
        out Vector3 forward)
    {
        track = null!;
        aim = default;
        forward = Vector3.forward;

        try
        {
            RailTrack? bestAny = null;
            RailTrack? bestNamed = null;
            var bestAnyDist = float.MaxValue;
            var bestNamedDist = float.MaxValue;

            foreach (var rail in tracks)
            {
                if (rail == null)
                {
                    continue;
                }

                var pointDist = RailTrack.GetClosestPoint(rail, look, 0f);
                var dist = pointDist.Item2;
                if (dist > MaxTrackSnapMeters)
                {
                    continue;
                }

                if (dist < bestAnyDist)
                {
                    bestAnyDist = dist;
                    bestAny = rail;
                }

                var key = PathGraphBuilder.TrackKey(rail);
                if (key != null
                    && !PathRouteConstraints.IsAnonymousTrack(key)
                    && dist < bestNamedDist)
                {
                    bestNamedDist = dist;
                    bestNamed = rail;
                }
            }

            RailTrack? pick;
            if (bestAny != null)
            {
                var anyKey = PathGraphBuilder.TrackKey(bestAny);
                var anyAnon = anyKey != null && PathRouteConstraints.IsAnonymousTrack(anyKey);
                if (anyAnon
                    && bestNamed != null
                    && bestNamedDist <= bestAnyDist + 2.5f)
                {
                    pick = bestNamed;
                }
                else
                {
                    pick = bestAny;
                }
            }
            else
            {
                return false;
            }

            var closest = RailTrack.GetClosestPoint(pick, look, 0f);
            if (closest.Item1 is not { } point)
            {
                return false;
            }

            aim = (Vector3)point.position;
            forward = point.forward;
            track = pick;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Own a private CarDestinationHighlighter built from the game's own highlighter prefab +
    /// place materials. The live radio/re-rail instances are only constructed while those modes
    /// are awake, so they cannot be borrowed; the ctor re-parents to WorldMover.OriginShiftParent,
    /// which is what keeps the box glued to the rail across origin shifts.
    /// </summary>
    private static bool EnsureGhost()
    {
        if (_ghost != null && _ghostGo != null && _validMat != null)
        {
            return true;
        }

        try
        {
            GameObject? templateBox = null;
            GameObject? templateArrow = null;
            var source = "none";

            // FindObjectsOfTypeAll also returns inactive objects and loaded prefab assets.
            foreach (var crew in Resources.FindObjectsOfTypeAll<CommsRadioCrewVehicle>())
            {
                if (crew == null || crew.destinationHighlighterGO == null || crew.validMaterial == null)
                {
                    continue;
                }

                templateBox = crew.destinationHighlighterGO;
                templateArrow = crew.directionArrowsHighlighterGO;
                _validMat = crew.validMaterial;
                _invalidMat = crew.invalidMaterial;
                source = "CommsRadioCrewVehicle";
                break;
            }

            if (templateBox == null)
            {
                foreach (var rerail in Resources.FindObjectsOfTypeAll<RerailController>())
                {
                    if (rerail == null
                        || rerail.rerailDestinationHighlighterGO == null
                        || rerail.validMaterial == null)
                    {
                        continue;
                    }

                    templateBox = rerail.rerailDestinationHighlighterGO;
                    templateArrow = rerail.directionArrowsHighlighterGO;
                    _validMat = rerail.validMaterial;
                    _invalidMat = rerail.invalidMaterial;
                    source = "RerailController";
                    break;
                }
            }

            if (templateBox == null || _validMat == null)
            {
                if (!_ghostMissLogged)
                {
                    _ghostMissLogged = true;
                    var crewCount = Resources.FindObjectsOfTypeAll<CommsRadioCrewVehicle>().Length;
                    var rerailCount = Resources.FindObjectsOfTypeAll<RerailController>().Length;
                    Main.Log(
                        "T2 loco-spawn: WARNING no highlighter template "
                        + $"(crew={crewCount} rerail={rerailCount})");
                }

                return false;
            }

            _invalidMat ??= _validMat;

            _ghostGo = UnityEngine.Object.Instantiate(templateBox);
            _ghostGo.name = "YMS_LocoSpawnGhost";
            if (templateArrow != null)
            {
                _ghostArrowGo = UnityEngine.Object.Instantiate(templateArrow);
                _ghostArrowGo.name = "YMS_LocoSpawnGhostArrow";
            }

            _ghost = new CarDestinationHighlighter(_ghostGo, _ghostArrowGo!);
            _ghostMissLogged = false;
            Main.Log($"T2 loco-spawn: ghost built from {source} · box={templateBox.name}");
            return true;
        }
        catch (Exception ex)
        {
            Main.Log("T2 loco-spawn: ghost build fail " + ex.GetType().Name + " " + ex.Message);
            DisposeGhost();
            return false;
        }
    }

    private static void UpdateGhost(Vector3 position, Vector3 forward, Vector3 halfExtents, bool ok)
    {
        if (!EnsureGhost() || _ghost == null)
        {
            return;
        }

        try
        {
            // Highlight() sets localScale from bounds.size and lifts the box to sit on the rail.
            var size = new Vector3(
                Mathf.Clamp(halfExtents.x * 2f, 2f, 4.5f),
                Mathf.Clamp(halfExtents.y * 2f, 2.5f, 5f),
                Mathf.Clamp(halfExtents.z * 2f, 8f, 18f));
            _ghost.Highlight(position, forward, new Bounds(Vector3.zero, size), ok ? _validMat! : _invalidMat!);
        }
        catch (Exception ex)
        {
            Main.Log("T2 loco-spawn: Highlight fail " + ex.GetType().Name);
        }
    }

    private static void TurnOffGhost()
    {
        try
        {
            if (_ghostGo != null)
            {
                _ghost?.TurnOff();
            }
        }
        catch
        {
            // ignore
        }
    }

    public static void DisposeGhost()
    {
        try
        {
            _ghost?.Destroy();
        }
        catch
        {
            // ignore
        }

        _ghost = null;
        _ghostGo = null;
        _ghostArrowGo = null;
    }
}
