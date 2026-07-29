using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Dispatch desk (3.5): city/track dropdowns. Path only on Set dest / Check / Align.
/// </summary>
internal sealed class DispatchDeskPanel : MonoBehaviour
{
    private bool _visible;
    private List<(string YardId, string TrackId)> _catalog = new();
    private IReadOnlyList<string> _yards = System.Array.Empty<string>();
    private IReadOnlyList<string> _tracks = System.Array.Empty<string>();
    private int _yardIndex;
    private int _trackIndex;
    private string _status = "";
    private float _originWatchAt;
    private bool _yardDropOpen;
    private bool _trackDropOpen;
    private Vector2 _yardScroll;
    private Vector2 _trackScroll;

    private void Update()
    {
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            _visible = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Insert))
        {
            _visible = !_visible;
            if (_visible)
            {
                RefreshCatalog(force: true);
                SyncIndicesFromSession();
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
        if (!_visible || !HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            return;
        }

        const float w = 400f;
        const float h = 280f;
        var x = (Screen.width - w) * 0.5f;
        var y = Screen.height * 0.14f;
        GUI.Box(new Rect(x, y, w, h), "Dispatch desk (Dispatcher · Align Route)");

        var yard = _yards.Count > 0 ? _yards[_yardIndex] : "— pick city —";
        var track = _tracks.Count > 0 ? _tracks[_trackIndex] : "— pick track —";
        var planChip = TelemetryReader.CurrentPathCheckLabel()
            ?? (RouteDestSession.HasDestination ? "Path (Set dest / Check)" : "Path —");
        var facing = TelemetryReader.CurrentFacingLabel() ?? "Facing —";
        var exit = RoutePlanSession.ExitCue ?? "Exit —";
        var license = RouteAlignAccess.DeniedChip(RouteAlignGovernor.HasDispatcherLicense())
            ?? "Dispatcher ok";

        var row = y + 28f;
        GUI.Label(new Rect(x + 12, row, 50, 22), "City");
        if (GUI.Button(new Rect(x + 70, row, 200, 24), yard + " ▼"))
        {
            _yardDropOpen = !_yardDropOpen;
            _trackDropOpen = false;
            if (_yards.Count == 0)
            {
                RefreshCatalog(force: true);
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
            _status = _yards.Count > 0
                ? $"{_yards.Count} cities · {PathGraphBuilder.LastDiag}"
                : ("empty · " + PathGraphBuilder.LastDiag);
        }

        if (!string.IsNullOrEmpty(_status))
        {
            GUI.Label(new Rect(x + 270, row, w - 282, 28), _status);
        }
    }

    private void RefreshCatalog(bool force)
    {
        if (force)
        {
            PathGraphBuilder.InvalidateCache();
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
