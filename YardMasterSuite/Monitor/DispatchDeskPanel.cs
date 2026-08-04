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
            PathGraphBuilder.EnsureMappingStarted();
            Main.Log("T2 path: session map warm started");
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

        if (!_visible)
        {
            return;
        }

        const float w = 420f;
        var h = _mode == DeskMode.SwitchList ? 360f : (PathGraphBuilder.IsMapping ? 300f : 280f);
        var x = (Screen.width - w) * 0.5f;
        var y = Screen.height * 0.12f;
        GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher)");

        var row = y + 26f;
        if (GUI.Button(new Rect(x + 12, row, 100, 22), _mode == DeskMode.Route ? "● Route" : "Route"))
        {
            _mode = DeskMode.Route;
            _jobDropOpen = false;
        }

        if (GUI.Button(new Rect(x + 118, row, 120, 22), _mode == DeskMode.SwitchList ? "● Switch List" : "Switch List"))
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
        var exit = RoutePlanSession.ExitCue ?? "Exit —";
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
            if (!ApplySelection())
            {
                _status = _yards.Count == 0 ? "no cities — reopen in world" : "pick city + track";
                RefreshCatalog(force: true);
            }
            else
            {
                _status = RoutePlanService.Compute("set");
                Main.Log(_status);
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
        }

        row += 34f;
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

        if (!string.IsNullOrEmpty(_status))
        {
            GUI.Label(new Rect(x + 270, row, w - 282, 28), _status);
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
            if (SwitchListSession.TryAdvance())
            {
                var step = SwitchListSession.CurrentStep;
                _status = step != null ? $"step {step.Index}: {step.Label}" : "advanced";
                Main.Log("T2 switch-list: next · " + _status);
            }
            else if (SwitchListSession.IsComplete)
            {
                _status = "Switch List complete";
                Main.Log("T2 switch-list: complete");
            }
            else
            {
                _status = "no Switch List";
            }
        }

        if (GUI.Button(new Rect(x + 330, row, 70, 26), "Clear"))
        {
            SwitchListSession.Clear();
            _status = "list cleared";
            Main.Log("T2 switch-list: cleared");
        }

        row += 30f;

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
        else
        {
            GUI.Label(new Rect(x + 12, row, w - 24, 40), "Pick a taken or held job → Load Switch List → Align step per leg.");
            row += 44f;
        }

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
        _tracks = DestinationCatalog.ListTracksInYard(_catalog, yard);
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
            for (var i = 0; i < _tracks.Count; i++)
            {
                if (string.Equals(_tracks[i], RouteDestSession.TrackId, System.StringComparison.OrdinalIgnoreCase))
                {
                    _trackIndex = i;
                    break;
                }
            }
        }
    }

    private bool ApplySelection()
    {
        if (_yards.Count == 0 || _tracks.Count == 0)
        {
            return false;
        }

        RouteDestSession.Set(_yards[_yardIndex], _tracks[_trackIndex]);
        return true;
    }
}
