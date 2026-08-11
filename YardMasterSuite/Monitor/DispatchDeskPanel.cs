using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Dispatch desk: city/track Align (3.5) + Digital Switch List (3.6).
/// Path / Align only on button press.
/// </summary>
internal sealed class DispatchDeskPanel : MonoBehaviour
{
    private enum DeskMode
    {
        Route,
        SwitchList,
    }

    private bool _visible;
    private DeskMode _mode = DeskMode.Route;
    private static bool _requestSwitchListTab;
    private List<(string YardId, string TrackId)> _catalog = new();
    private IReadOnlyList<string> _yards = System.Array.Empty<string>();
    private IReadOnlyList<string> _tracks = System.Array.Empty<string>();
    private int _yardIndex;
    private int _trackIndex;
    private string _status = "";
    private float _originWatchAt;
    private bool _yardDropOpen;
    private bool _trackDropOpen;
    private bool _jobDropOpen;
    private Vector2 _yardScroll;
    private Vector2 _trackScroll;
    private Vector2 _jobScroll;
    private Vector2 _stepScroll;
    private bool _worldSessionActive;

    private List<Job> _jobs = new();
    private int _jobIndex;

    /// <summary>Job load — show Per job tab next draw.</summary>
    internal static void RequestSwitchListTab() => _requestSwitchListTab = true;

    private void Update()
    {
        var world = HudWorldSession.IsActive(PlayerManager.PlayerTransform != null);
        if (!world)
        {
            if (_worldSessionActive)
            {
                PathGraphBuilder.InvalidateCache();
                _worldSessionActive = false;
            }

            _visible = false;
            return;
        }

        if (!_worldSessionActive)
        {
            _worldSessionActive = true;
            // Do NOT EnsureMappingStarted here — pumping ~2k tracks on every world enter
            // reintroduces the rhythmic hitch. Warm on first sticky city / desk / Align / Set Dest.
            Main.Log("T2 path: world session (map warm deferred until sticky city/desk/Align/Set Dest)");
        }

        if (PathGraphBuilder.IsMapping)
        {
            var finished = PathGraphBuilder.TickMapping();
            if (_visible)
            {
                _status = PathGraphBuilder.MappingBanner;
            }

            if (finished)
            {
                Main.Log("T2 path: session map ready · " + PathGraphBuilder.LastDiag);
                if (_visible && PathGraphBuilder.HasReadyCache)
                {
                    RefreshCatalog(force: false);
                    SyncIndicesFromSession();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Insert))
        {
            _visible = !_visible;
            if (_visible)
            {
                if (PathGraphBuilder.HasReadyCache)
                {
                    RefreshCatalog(force: false);
                    SyncIndicesFromSession();
                }
                else
                {
                    _catalog = new List<(string, string)>();
                    _yards = System.Array.Empty<string>();
                    _tracks = System.Array.Empty<string>();
                    PathGraphBuilder.EnsureMappingStarted();
                    if (PathGraphBuilder.HasReadyCache)
                    {
                        RefreshCatalog(force: false);
                        SyncIndicesFromSession();
                    }
                    else
                    {
                        _status = PathGraphBuilder.MappingBanner.Length > 0
                            ? PathGraphBuilder.MappingBanner
                            : "Station mapping…";
                    }
                }

                if (_mode == DeskMode.SwitchList)
                {
                    RefreshJobs();
                }
            }
        }

        if (Time.unscaledTime - _originWatchAt > 0.5f)
        {
            _originWatchAt = Time.unscaledTime;
            var drift = RoutePlanService.WatchPathDrift();
            if (drift != null)
            {
                Main.Log(drift);
                _status = RoutePlanSession.StatusMessage ?? drift;
            }
        }

        if (JobCarsPlaceSession.IsActive)
        {
            PollPlaceTarget();
        }
    }

    private void PollPlaceTarget()
    {
        // Look-at wins: camera ray → track near the hit (not "closest to feet").
        const float maxLookMeters = 250f;
        const float maxTrackSnapMeters = 12f;

        try
        {
            var cam = PlayerManager.PlayerCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                JobCarsPlaceSession.ClearAim();
                return;
            }

            var tracks = RailTrackRegistry.RailTracks;
            if (tracks == null || tracks.Length == 0)
            {
                JobCarsPlaceSession.ClearAim();
                return;
            }

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            Vector3 aim;
            if (Physics.Raycast(ray, out var hit, maxLookMeters, ~0, QueryTriggerInteraction.Ignore))
            {
                aim = hit.point;
            }
            else
            {
                aim = ray.GetPoint(40f);
            }

            // Rank tracks by distance from aim; prefer named (FF-…) over anonymous #Y.
            RailTrack? bestNamed = null;
            var bestNamedDist = float.MaxValue;
            RailTrack? bestAny = null;
            var bestAnyDist = float.MaxValue;

            for (var i = 0; i < tracks.Length; i++)
            {
                var rail = tracks[i];
                if (rail == null)
                {
                    continue;
                }

                var pointDist = RailTrack.GetClosestPoint(rail, aim, 0f);
                var dist = pointDist.Item2;
                if (dist > maxTrackSnapMeters)
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

            // Closest-to-aim wins. If that is anonymous (#Y) and a named track is nearly as close, prefer named.
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
                JobCarsPlaceSession.ClearAim();
                return;
            }

            var pickKey = PathGraphBuilder.TrackKey(pick);
            JobCarsPlaceSession.SetTargetTrack(pickKey);
            JobCarsPlaceSession.SetAimPoint(aim.x, aim.y, aim.z);
        }
        catch
        {
            JobCarsPlaceSession.ClearAim();
        }
    }

    private void OnGUI()
    {
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            return;
        }

        if (PathGraphBuilder.IsMapping)
        {
            DrawMappingBanner();
        }

        if (_requestSwitchListTab)
        {
            _requestSwitchListTab = false;
            _mode = DeskMode.SwitchList;
            _visible = true;
            _yardDropOpen = _trackDropOpen = _jobDropOpen = false;
        }

        if (!_visible)
        {
            return;
        }

        const float w = 420f;
        var stepCount = SwitchListSession.Steps?.Count ?? 0;
        var h = _mode == DeskMode.SwitchList
            ? 420f
            : (PathGraphBuilder.IsMapping ? 300f : (stepCount > 0 ? 450f : 310f));
        var x = (Screen.width - w) * 0.5f;
        var y = Screen.height * 0.12f;
        GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher)");

        var row = y + 26f;
        if (GUI.Button(new Rect(x + 12, row, 100, 22), _mode == DeskMode.Route ? "● Route" : "Route"))
        {
            _mode = DeskMode.Route;
            _jobDropOpen = false;
        }

        if (GUI.Button(new Rect(x + 118, row, 120, 22), _mode == DeskMode.SwitchList ? "● Per job" : "Per job"))
        {
            _mode = DeskMode.SwitchList;
            _yardDropOpen = _trackDropOpen = false;
            RefreshJobs();
        }

        row += 28f;
        if (_mode == DeskMode.SwitchList)
        {
            DrawSwitchList(x, ref row, w);
        }
        else
        {
            DrawRoute(x, ref row, w);
        }
    }

    private void DrawRoute(float x, ref float row, float w)
    {
        var yard = _yards.Count > 0 ? _yards[_yardIndex] : "— pick city —";
        var track = _tracks.Count > 0 ? _tracks[_trackIndex] : "— pick track —";
        var planChip = TelemetryReader.CurrentPathCheckLabel()
            ?? (RouteDestSession.HasDestination ? "Path (Set dest / Check)" : "Path —");
        var facing = TelemetryReader.CurrentFacingLabel() ?? "Facing —";
        var exit = TelemetryReader.CurrentExitLabel() ?? "Exit —";
        var license = RouteAlignAccess.DeniedChip(RouteAlignGovernor.HasDispatcherLicense())
            ?? "Dispatcher ok";

        if (PathGraphBuilder.IsMapping)
        {
            GUI.Label(new Rect(x + 12, row, w - 24, 22), PathGraphBuilder.MappingBanner);
            row += 26f;
        }

        GUI.Label(new Rect(x + 12, row, 50, 22), "City");
        if (GUI.Button(new Rect(x + 70, row, 200, 24), yard + " ▼"))
        {
            _yardDropOpen = !_yardDropOpen;
            _trackDropOpen = false;
            if (_yards.Count == 0 && !PathGraphBuilder.IsMapping)
            {
                PathGraphBuilder.EnsureMappingStarted();
                _status = PathGraphBuilder.MappingBanner.Length > 0
                    ? PathGraphBuilder.MappingBanner
                    : "Station mapping…";
            }
        }

        row += 28f;
        if (_yardDropOpen && _yards.Count > 0)
        {
            var dropH = Mathf.Min(140f, 22f * _yards.Count + 8f);
            _yardScroll = GUI.BeginScrollView(
                new Rect(x + 70, row, 200, dropH),
                _yardScroll,
                new Rect(0, 0, 180, 22f * _yards.Count));
            for (var i = 0; i < _yards.Count; i++)
            {
                if (GUI.Button(new Rect(0, i * 22f, 180, 22), _yards[i]))
                {
                    _yardIndex = i;
                    _trackIndex = 0;
                    RefreshTracks();
                    _yardDropOpen = false;
                }
            }

            GUI.EndScrollView();
            row += dropH + 4f;
        }

        GUI.Label(new Rect(x + 12, row, 50, 22), "Track");
        if (GUI.Button(new Rect(x + 70, row, 280, 24), track + " ▼"))
        {
            _trackDropOpen = !_trackDropOpen;
            _yardDropOpen = false;
            if (_tracks.Count == 0)
            {
                RefreshTracks();
            }
        }

        row += 28f;
        if (_trackDropOpen && _tracks.Count > 0)
        {
            var dropH = Mathf.Min(160f, 22f * _tracks.Count + 8f);
            _trackScroll = GUI.BeginScrollView(
                new Rect(x + 70, row, 280, dropH),
                _trackScroll,
                new Rect(0, 0, 260, 22f * _tracks.Count));
            for (var i = 0; i < _tracks.Count; i++)
            {
                if (GUI.Button(new Rect(0, i * 22f, 260, 22), _tracks[i]))
                {
                    _trackIndex = i;
                    _trackDropOpen = false;
                }
            }

            GUI.EndScrollView();
            row += dropH + 4f;
        }

        GUI.Label(new Rect(x + 12, row, w - 24, 22), $"{planChip}  |  {facing}  |  {exit}");
        row += 22f;
        GUI.Label(new Rect(x + 12, row, w - 24, 22), license);
        row += 28f;

        if (GUI.Button(new Rect(x + 12, row, 100, 28), "Set dest"))
        {
            _yardDropOpen = _trackDropOpen = false;
            if (_yards.Count == 0 || _tracks.Count == 0)
            {
                _status = _yards.Count == 0 ? "no cities — reopen in world" : "pick city + track";
                RefreshCatalog(force: true);
            }
            else
            {
                _status = DispatchDeskSetDest.Run(_yards[_yardIndex], _tracks[_trackIndex]);
                if (RouteDestSession.HasDestination)
                {
                    SyncIndicesFromSession();
                }
            }
        }

        if (GUI.Button(new Rect(x + 118, row, 100, 28), "Recheck"))
        {
            _yardDropOpen = _trackDropOpen = false;
            if (!RouteDestSession.HasDestination)
            {
                ApplySelection();
            }

            if (!RouteDestSession.HasDestination)
            {
                _status = "pick city + track";
            }
            else
            {
                _status = RoutePlanService.Compute("recheck");
                Main.Log(_status);
                DispatchDeskSetDest.LogRouteSnapshot("recheck");
            }
        }

        if (GUI.Button(new Rect(x + 224, row, 100, 28), "Align Route"))
        {
            _yardDropOpen = _trackDropOpen = false;
            if (!RouteDestSession.HasDestination)
            {
                ApplySelection();
            }

            var line = RouteAlignGovernor.TryAlign();
            _status = line ?? "";
            if (line != null)
            {
                Main.Log(line);
            }

            DispatchDeskSetDest.LogRouteSnapshot("align", line);
        }

        row += 34f;

        // Multi-leg list lives on Route too — need Next/Align here (smoke: Arrived · Next, no button).
        var hasSteps = SwitchListSession.Steps != null && SwitchListSession.Steps.Count > 0;
        if (hasSteps)
        {
            if (GUI.Button(new Rect(x + 12, row, 100, 28), "Align step"))
            {
                _yardDropOpen = _trackDropOpen = false;
                AlignCurrentStep();
            }

            if (GUI.Button(new Rect(x + 118, row, 70, 28), "Next"))
            {
                _yardDropOpen = _trackDropOpen = false;
                AdvanceSwitchListStep();
            }

            row += 34f;
        }

        if (GUI.Button(new Rect(x + 12, row, 70, 28), "Clear"))
        {
            RoutePlanService.ClearAll();
            _status = "cleared";
            Main.Log("T2 path: cleared");
        }

        if (GUI.Button(new Rect(x + 90, row, 70, 28), "Hide"))
        {
            _visible = false;
        }

        if (GUI.Button(new Rect(x + 170, row, 90, 28), "Reload list"))
        {
            RefreshCatalog(force: true);
            if (PathGraphBuilder.IsMapping)
            {
                _status = PathGraphBuilder.MappingBanner;
            }
            else
            {
                _status = _yards.Count > 0
                    ? $"{_yards.Count} cities · {PathGraphBuilder.LastDiag}"
                    : ("empty · " + PathGraphBuilder.LastDiag);
            }
        }

        if (GUI.Button(new Rect(x + 268, row, 100, 28), "Dump graph"))
        {
            _yardDropOpen = _trackDropOpen = false;
            DumpYardGraph();
        }

        row += 32f;
        if (!string.IsNullOrEmpty(_status))
        {
            GUI.Label(new Rect(x + 12, row, w - 24, 28), _status);
            row += 28f;
        }

        DrawActiveSteps(x, ref row, w, emptyHint: null);
    }

    private void DrawActiveSteps(float x, ref float row, float w, string? emptyHint)
    {
        var steps = SwitchListSession.Steps;
        if (steps != null && steps.Count > 0)
        {
            var active = SwitchListSession.JobId ?? "";
            var cur = SwitchListSession.IsComplete
                ? "done"
                : (SwitchListSession.CurrentStep?.Label ?? "—");
            GUI.Label(new Rect(x + 12, row, w - 24, 20), $"{active} · {cur}");
            row += 22f;

            var listH = Mathf.Min(120f, 20f * steps.Count + 4f);
            _stepScroll = GUI.BeginScrollView(
                new Rect(x + 12, row, w - 24, listH),
                _stepScroll,
                new Rect(0, 0, w - 48, 20f * steps.Count));
            for (var i = 0; i < steps.Count; i++)
            {
                var mark = i == SwitchListSession.CurrentIndex && !SwitchListSession.IsComplete ? "▶ " : "  ";
                GUI.Label(new Rect(0, i * 20f, w - 48, 20), mark + steps[i].Label);
            }

            GUI.EndScrollView();
            row += listH + 4f;
        }
        else if (!string.IsNullOrEmpty(emptyHint))
        {
            GUI.Label(new Rect(x + 12, row, w - 24, 40), emptyHint);
            row += 44f;
        }
    }

    private void DrawSwitchList(float x, ref float row, float w)
    {
        var license = RouteAlignAccess.DeniedChip(RouteAlignGovernor.HasDispatcherLicense())
            ?? "Dispatcher ok";
        GUI.Label(new Rect(x + 12, row, w - 24, 20), license);
        row += 22f;

        var jobLabel = _jobs.Count > 0 && _jobIndex < _jobs.Count
            ? (_jobs[_jobIndex].ID ?? "job")
            : "— no jobs (taken / held) —";
        GUI.Label(new Rect(x + 12, row, 40, 22), "Job");
        if (GUI.Button(new Rect(x + 55, row, 240, 24), jobLabel + " ▼"))
        {
            _jobDropOpen = !_jobDropOpen;
            RefreshJobs();
        }

        if (GUI.Button(new Rect(x + 300, row, 100, 24), "Refresh"))
        {
            RefreshJobs();
            _status = $"{_jobs.Count} jobs";
        }

        row += 28f;
        if (_jobDropOpen && _jobs.Count > 0)
        {
            var dropH = Mathf.Min(110f, 22f * _jobs.Count + 8f);
            _jobScroll = GUI.BeginScrollView(
                new Rect(x + 55, row, 240, dropH),
                _jobScroll,
                new Rect(0, 0, 220, 22f * _jobs.Count));
            for (var i = 0; i < _jobs.Count; i++)
            {
                var id = _jobs[i].ID ?? $"job{i}";
                if (GUI.Button(new Rect(0, i * 22f, 220, 22), id))
                {
                    _jobIndex = i;
                    _jobDropOpen = false;
                }
            }

            GUI.EndScrollView();
            row += dropH + 4f;
        }

        if (GUI.Button(new Rect(x + 12, row, 130, 26), "Load Switch List"))
        {
            _jobDropOpen = false;
            LoadSelectedJob();
        }

        if (GUI.Button(new Rect(x + 148, row, 100, 26), "Align step"))
        {
            AlignCurrentStep();
        }

        if (GUI.Button(new Rect(x + 254, row, 70, 26), "Next"))
        {
            AdvanceSwitchListStep();
        }

        if (GUI.Button(new Rect(x + 330, row, 70, 26), "Clear"))
        {
            SwitchListSession.Clear();
            _status = "list cleared";
            Main.Log("T2 switch-list: cleared");
        }

        row += 30f;

        // 3.1 — move existing job cars (re-rail-style place)
        if (GUI.Button(new Rect(x + 12, row, 150, 26), "Move job cars here"))
        {
            _jobDropOpen = false;
            RefreshJobs();
            var job = _jobs.Count > 0 && _jobIndex < _jobs.Count ? _jobs[_jobIndex] : null;
            _status = JobCarsTeleportGovernor.BeginPlaceForJob(job);
        }

        if (GUI.Button(new Rect(x + 168, row, 90, 26), "Confirm place"))
        {
            _status = JobCarsTeleportGovernor.ConfirmPlace(this);
        }

        if (GUI.Button(new Rect(x + 264, row, 70, 26), "Flip"))
        {
            if (JobCarsPlaceSession.IsActive)
            {
                JobCarsPlaceSession.ToggleFacing();
                _status = JobCarsPlaceSession.ForceRegularDirection ? "facing regular" : "facing flipped";
            }
        }

        if (GUI.Button(new Rect(x + 340, row, 60, 26), "Cancel"))
        {
            _status = JobCarsTeleportGovernor.CancelPlace();
        }

        row += 28f;
        if (JobCarsPlaceSession.IsActive)
        {
            var abort = JobCarsTeleportAbort.None;
            if (string.IsNullOrEmpty(JobCarsPlaceSession.TargetTrackId))
            {
                abort = JobCarsTeleportAbort.NoTarget;
            }

            var chip = JobCarsTeleportPolicy.FormatPlaceChip(
                true,
                JobCarsPlaceSession.ExpectedCars,
                JobCarsPlaceSession.TargetTrackId,
                abort);
            GUI.Label(new Rect(x + 12, row, w - 24, 20), chip);
            row += 22f;
        }

        if (GUI.Button(new Rect(x + 12, row, 100, 26), "Snap office"))
        {
            _status = TryStationSnap();
        }

        if (GUI.Button(new Rect(x + 118, row, 100, 26), "Return"))
        {
            _status = TryStationReturn();
        }

        row += 30f;

        DrawActiveSteps(
            x,
            ref row,
            w,
            emptyHint: "Pick a taken or held job → Load list → Align step per leg.");

        var planChip = TelemetryReader.CurrentPathCheckLabel() ?? "Path —";
        var facing = TelemetryReader.CurrentFacingLabel() ?? "Facing —";
        GUI.Label(new Rect(x + 12, row, w - 24, 20), $"{planChip}  |  {facing}");
        row += 22f;

        if (GUI.Button(new Rect(x + 12, row, 70, 26), "Hide"))
        {
            _visible = false;
        }

        if (!string.IsNullOrEmpty(_status))
        {
            GUI.Label(new Rect(x + 90, row, w - 102, 26), _status);
        }
    }

    private void RefreshJobs()
    {
        _jobs = new List<Job>(SwitchListJobReader.ListCandidateJobs());
        if (_jobIndex >= _jobs.Count)
        {
            _jobIndex = 0;
        }
    }

    private void LoadSelectedJob()
    {
        RefreshJobs();
        if (_jobs.Count == 0 || _jobIndex >= _jobs.Count)
        {
            _status = "no jobs";
            Main.Log("T2 switch-list: no jobs");
            return;
        }

        var job = _jobs[_jobIndex];
        if (!SwitchListJobReader.TryBuildSummary(job, out var summary, out var error) || summary == null)
        {
            _status = error ?? "cannot read job tracks";
            Main.Log("T2 switch-list: " + _status);
            SwitchListSession.Clear();
            return;
        }

        var steps = SwitchListPlanner.Build(summary);
        if (steps == null || steps.Count == 0)
        {
            _status = "planner fail-closed";
            Main.Log("T2 switch-list: planner fail-closed · " + summary.JobId);
            SwitchListSession.Clear();
            return;
        }

        SwitchListSession.Bind(summary.JobId, steps);
        _status = $"loaded {steps.Count} steps · {summary.JobId}";
        Main.Log($"T2 switch-list: loaded {summary.JobId} · {steps.Count} steps · {summary.OriginTrackId} → {summary.DestTrackId}");
    }

    private void AdvanceSwitchListStep()
    {
        if (!SwitchListSession.HasActive)
        {
            _status = "no list";
            return;
        }

        if (SwitchListSession.IsComplete)
        {
            _status = "list complete";
            return;
        }

        if (!TelemetryReader.IsSwitchListStepArrived())
        {
            _status = "wait · clear the switch / get nearer the pin";
            Main.Log("T2 switch-list: next blocked · consist not clear");
            return;
        }

        if (!SwitchListSession.TryAdvance())
        {
            _status = SwitchListSession.IsComplete ? "list complete" : "no list";
            if (SwitchListSession.IsComplete)
            {
                Main.Log("T2 switch-list: complete");
            }

            return;
        }

        var step = SwitchListSession.CurrentStep;
        if (step != null && !string.IsNullOrEmpty(step.DestTrackId))
        {
            RouteDestSession.Set(step.DestYardId, step.DestTrackId);
            RoutePlanService.Compute("list-next");
            var driveSet = SwitchListDriveFacing.SetWord(
                RouteFacingResolver.IsTargetBehind(RoutePlanSession.Plan));
            var align = RouteAlignGovernor.TryAlign() ?? "align —";
            _status = $"step {step.Index}: {step.Label} · {driveSet} · {align}";
            DispatchDeskSetDest.LogRouteSnapshot("next", _status);
            return;
        }

        _status = step != null ? $"step {step.Index}: {step.Label}" : "advanced";
        Main.Log("T2 switch-list: next · " + _status);
    }

    private void AlignCurrentStep()
    {
        if (!SwitchListSession.HasActive || SwitchListSession.IsComplete)
        {
            _status = "no active step";
            return;
        }

        var step = SwitchListSession.CurrentStep;
        if (step == null || string.IsNullOrEmpty(step.DestTrackId))
        {
            _status = "no step track";
            return;
        }

        RouteDestSession.Set(step.DestYardId, step.DestTrackId);
        var line = RouteAlignGovernor.TryAlign();
        _status = line ?? $"aligned → {step.DestTrackId}";
        Main.Log($"T2 switch-list: align step {step.Index} {step.Kind} · {_status}");
    }

    private static string TryStationSnap()
    {
        try
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                return "T2 snap: no player";
            }

            if (!TelemetryReader.TryGetArStationOfficeWorldPosition(out var office))
            {
                return "T2 snap: no office in zone";
            }

            StationSnapSession.CaptureReturn(player.position.x, player.position.y, player.position.z);
            player.position = office;
            Main.Log("T2 snap: to office");
            return "snapped to office";
        }
        catch (System.Exception ex)
        {
            Main.Log("T2 snap: fail · " + ex.GetType().Name);
            return "snap failed";
        }
    }

    private static string TryStationReturn()
    {
        try
        {
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                return "T2 snap: no player";
            }

            if (!StationSnapSession.TryGetReturn(out var x, out var y, out var z))
            {
                return "T2 snap: no return point";
            }

            player.position = new Vector3(x, y, z);
            StationSnapSession.Clear();
            Main.Log("T2 snap: returned");
            return "returned";
        }
        catch (System.Exception ex)
        {
            Main.Log("T2 snap: return fail · " + ex.GetType().Name);
            return "return failed";
        }
    }

    private static void DrawMappingBanner()
    {
        var banner = PathGraphBuilder.MappingBanner;
        if (string.IsNullOrEmpty(banner))
        {
            banner = "Station mapping…";
        }

        const float bw = 420f;
        const float bh = 48f;
        var bx = (Screen.width - bw) * 0.5f;
        var by = Screen.height * 0.06f;
        GUI.Box(new Rect(bx, by, bw, bh), banner);
    }

    private void DumpYardGraph()
    {
        if (!PathGraphBuilder.HasReadyCache)
        {
            PathGraphBuilder.EnsureMappingStarted();
            _status = PathGraphBuilder.IsMapping
                ? PathGraphBuilder.MappingBanner
                : ("Dump graph: map first · " + PathGraphBuilder.LastDiag);
            return;
        }

        var yard = _yards.Count > 0 ? _yards[_yardIndex] : null;
        var trackOrToken = _tracks.Count > 0 ? _tracks[_trackIndex] : null;
        string? tt = null;
        if (!string.IsNullOrEmpty(yard)
            && !string.IsNullOrEmpty(trackOrToken)
            && DispatchDeskSetDest.TryResolveTrackId(yard!, trackOrToken!, out var resolved, out _))
        {
            if (DispatchDeskSetDest.IsTurntableToken(trackOrToken))
            {
                tt = resolved;
            }
        }

        var path = YardGraphSnapshotWriter.TryDump(yard, TelemetryReaderOrigin.TryGet(), tt, out var err);
        if (path == null)
        {
            _status = "Dump graph failed · " + err;
            return;
        }

        _status = "Dump graph · " + System.IO.Path.GetFileName(path);
    }

    private void RefreshCatalog(bool force)
    {
        if (force)
        {
            PathGraphBuilder.InvalidateCache();
            PathGraphBuilder.EnsureMappingStarted();
            _catalog = new List<(string, string)>();
            _yards = System.Array.Empty<string>();
            _tracks = System.Array.Empty<string>();
            _status = PathGraphBuilder.MappingBanner.Length > 0
                ? PathGraphBuilder.MappingBanner
                : "Station mapping…";
            return;
        }

        if (!PathGraphBuilder.HasReadyCache)
        {
            PathGraphBuilder.EnsureMappingStarted();
            _catalog = new List<(string, string)>();
            _yards = System.Array.Empty<string>();
            _tracks = System.Array.Empty<string>();
            _status = PathGraphBuilder.IsMapping
                ? PathGraphBuilder.MappingBanner
                : ("no cities · " + PathGraphBuilder.LastDiag);
            return;
        }

        if (!PathGraphBuilder.TryBuild(out _, out _, out _, out var catalog) || catalog.Count == 0)
        {
            _catalog = new List<(string, string)>();
            _yards = System.Array.Empty<string>();
            _tracks = System.Array.Empty<string>();
            _status = "no cities · " + PathGraphBuilder.LastDiag;
            return;
        }

        _catalog = catalog;
        _yards = DestinationCatalog.ListYards(_catalog);
        if (_yardIndex >= _yards.Count)
        {
            _yardIndex = 0;
        }

        RefreshTracks();
        _status = $"{_yards.Count} cities / {_tracks.Count} tracks · {PathGraphBuilder.LastDiag}";
    }

    private void RefreshTracks()
    {
        var yard = _yards.Count > 0 ? _yards[_yardIndex] : null;
        var listed = DestinationCatalog.ListTracksInYard(_catalog, yard);
        if (!string.IsNullOrWhiteSpace(yard))
        {
            // Synthetic dest — resolved to a real TT rail on Set dest (no FoT until then).
            var withTt = new List<string>(listed.Count + 1) { DispatchDeskSetDest.TurntableToken };
            for (var i = 0; i < listed.Count; i++)
            {
                withTt.Add(listed[i]);
            }

            _tracks = withTt;
        }
        else
        {
            _tracks = listed;
        }

        if (_trackIndex >= _tracks.Count)
        {
            _trackIndex = 0;
        }
    }

    private void SyncIndicesFromSession()
    {
        if (RouteDestSession.YardId != null && _yards.Count > 0)
        {
            for (var i = 0; i < _yards.Count; i++)
            {
                if (string.Equals(_yards[i], RouteDestSession.YardId, System.StringComparison.OrdinalIgnoreCase))
                {
                    _yardIndex = i;
                    break;
                }
            }
        }

        RefreshTracks();
        if (RouteDestSession.TrackId != null && _tracks.Count > 0)
        {
            var dest = RouteDestSession.TrackId;
            var found = false;
            for (var i = 0; i < _tracks.Count; i++)
            {
                if (string.Equals(_tracks[i], dest, System.StringComparison.OrdinalIgnoreCase))
                {
                    _trackIndex = i;
                    found = true;
                    break;
                }
            }

            // TT dest ids are not in the catalog — keep Turntable token selected.
            if (!found
                && _tracks.Count > 0
                && DispatchDeskSetDest.IsTurntableToken(_tracks[0]))
            {
                _trackIndex = 0;
            }
        }
    }

    private bool ApplySelection()
    {
        if (_yards.Count == 0 || _tracks.Count == 0)
        {
            return false;
        }

        if (!DispatchDeskSetDest.TryResolveTrackId(
                _yards[_yardIndex],
                _tracks[_trackIndex],
                out var trackId,
                out _))
        {
            return false;
        }

        RouteDestSession.Set(_yards[_yardIndex], trackId);
        return true;
    }
}
