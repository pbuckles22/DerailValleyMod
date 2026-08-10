using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// In-world yard mini-map overlay (4.13). Hotkey <c>M</c> toggles; omit outside job zone / off-world.
/// Sticky yard while overlapping zones (MF + MFMB); pin clamps to panel when outside track AABB.
/// </summary>
internal sealed class YardMiniMapOverlay : MonoBehaviour
{
    private const KeyCode ToggleKey = KeyCode.M;
    private const float PanelWidth = 280f;
    private const float PanelHeight = 280f;
    private const float PanelMargin = 16f;
    private const float TitleHeight = 22f;
    private const float MapPadding = 8f;
    private const float RebuildIntervalSeconds = 2.5f;
    private const float PinRadius = 5f;
    private const float LandmarkRadius = 4f;
    private const float HeadingTickLength = 14f;
    private const float PinClampInset = 6f;
    private const float OffMapArrowSize = 10f;

    private static readonly Color PanelBg = new(0.06f, 0.08f, 0.1f, 0.82f);
    private static readonly Color TrackColor = new(0.55f, 0.6f, 0.65f, 0.95f);
    private static readonly Color PinColor = new(1f, 0.85f, 0.25f, 1f);
    private static readonly Color OfficeColor = new(0.45f, 0.85f, 0.5f, 1f);
    private static readonly Color TtColor = new(0.95f, 0.55f, 0.35f, 1f);

    private bool _visible;
    private bool _worldSessionActive;
    private GUIStyle? _titleStyle;
    private GUIStyle? _labelStyle;
    private Texture2D? _pixel;

    private YardMiniMapBuilder.Snapshot? _snapshot;
    private string? _snapshotYard;
    private string? _stickyYard;
    private float _nextRebuildAt = -999f;
    private readonly List<string> _inZoneYards = new(8);
    private readonly List<string> _fenceSatellites = new(4);
    private float _fenceCheckAt = -999f;

    private void OnDestroy()
    {
        if (_pixel != null)
        {
            Destroy(_pixel);
            _pixel = null;
        }
    }

    private void Update()
    {
        var world = HudWorldSession.IsActive(PlayerManager.PlayerTransform != null);
        if (!world)
        {
            if (_worldSessionActive)
            {
                _worldSessionActive = false;
                _visible = false;
                _snapshot = null;
                _snapshotYard = null;
                _stickyYard = null;
            }

            return;
        }

        if (!_worldSessionActive)
        {
            _worldSessionActive = true;
            // Map graph warm is on-demand (M / desk / Align) — never here.
        }

        // Only advance an in-flight pump (started by M/desk/Align); do not Tick cold.
        if (PathGraphBuilder.IsMapping)
        {
            PathGraphBuilder.TickMapping();
        }

        if (Input.GetKeyDown(ToggleKey))
        {
            _visible = !_visible;
            Main.Log(_visible ? "T2 minimap: on" : "T2 minimap: off");
            if (_visible)
            {
                PathGraphBuilder.EnsureMappingStarted();
            }
        }
    }

    private void OnGUI()
    {
        if (!_visible || !HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            return;
        }

        var yardId = ResolveStickyYard();
        if (string.IsNullOrWhiteSpace(yardId))
        {
            return;
        }

        EnsureStyles();
        EnsureSnapshot(yardId!);

        var panelX = Screen.width - PanelWidth - PanelMargin;
        var panelY = Screen.height - PanelHeight - PanelMargin;
        var panel = new Rect(panelX, panelY, PanelWidth, PanelHeight);
        GUI.color = Color.white;
        DrawFill(panel, PanelBg);

        var title = _snapshot != null ? "Yard " + _snapshot.YardId : "Yard " + yardId + " · Mapping…";
        GUI.Label(new Rect(panel.x + 8f, panel.y + 2f, panel.width - 16f, TitleHeight), title, _titleStyle);

        if (_snapshot == null)
        {
            return;
        }

        var map = new Rect(
            panel.x + MapPadding,
            panel.y + TitleHeight + 2f,
            panel.width - MapPadding * 2f,
            panel.height - TitleHeight - MapPadding - 2f);

        DrawTracks(map, _snapshot);
        DrawLandmarks(map, _snapshot);
        DrawPlayer(map, _snapshot);
    }

    private string? ResolveStickyYard()
    {
        TelemetryReader.CollectInZoneYardIds(_inZoneYards);
        if (_inZoneYards.Count == 0)
        {
            _stickyYard = null;
            _fenceSatellites.Clear();
            return null;
        }

        var now = Time.unscaledTime;
        float px = 0f, pz = 0f;
        var havePlayer = TelemetryReader.TryGetPlayerPosition(out px, out _, out pz);
        if (now >= _fenceCheckAt)
        {
            _fenceCheckAt = now + 0.5f;
            _fenceSatellites.Clear();
            if (havePlayer)
            {
                for (var i = 0; i < _inZoneYards.Count; i++)
                {
                    var id = _inZoneYards[i];
                    if (!YardMiniMapYardStick.IsSatelliteYard(id))
                    {
                        continue;
                    }

                    if (!TelemetryReader.TryGetOfficeForYard(id, out var ox, out var oz))
                    {
                        continue;
                    }

                    if (YardMiniMapYardStick.IsInsideOfficeFence(px, pz, ox, oz))
                    {
                        _fenceSatellites.Add(id);
                    }
                }
            }
        }

        TelemetryReader.TryGetInZoneYardAndOffice(out var nearest, out _, out _);
        var resolved = YardMiniMapYardStick.Resolve(
            _stickyYard,
            _inZoneYards,
            nearest,
            _fenceSatellites);

        if (!string.Equals(_stickyYard, resolved, System.StringComparison.OrdinalIgnoreCase))
        {
            LogResolveDetail(resolved, nearest, havePlayer, px, pz);
            TelemetryReader.OnLimitFiloTownChanged(_stickyYard, resolved);
        }

        _stickyYard = resolved;
        return resolved;
    }

    private void LogResolveDetail(string? resolved, string? nearest, bool havePlayer, float px, float pz)
    {
        var zones = string.Join(",", _inZoneYards);
        var fence = _fenceSatellites.Count == 0 ? "—" : string.Join(",", _fenceSatellites);
        var dist = "—";
        if (havePlayer && TelemetryReader.TryGetOfficeForYard("MFMB", out var ox, out var oz))
        {
            var dx = px - ox;
            var dz = pz - oz;
            dist = ((int)Mathf.Sqrt(dx * dx + dz * dz)).ToString();
        }

        Main.Log(
            "T2 minimap: detail sticky=" + (_stickyYard ?? "—")
            + " nearest=" + (nearest ?? "—")
            + " in=[" + zones + "]"
            + " fence=[" + fence + "]"
            + " distMFMB=" + dist + "m"
            + " → " + (resolved ?? "hide"));
    }

    private void EnsureSnapshot(string yardId)
    {
        var now = Time.unscaledTime;
        if (_snapshot != null
            && string.Equals(_snapshotYard, yardId, System.StringComparison.OrdinalIgnoreCase)
            && now < _nextRebuildAt)
        {
            return;
        }

        if (!PathGraphBuilder.HasReadyCache)
        {
            PathGraphBuilder.EnsureMappingStarted();
            _snapshot = null;
            _snapshotYard = yardId;
            return;
        }

        if (YardMiniMapBuilder.TryBuild(yardId, out var snap) && snap != null)
        {
            _snapshot = snap;
            _snapshotYard = yardId;
            _nextRebuildAt = now + RebuildIntervalSeconds;
        }
        else
        {
            _snapshot = null;
            _snapshotYard = yardId;
            _nextRebuildAt = now + 0.5f;
        }
    }

    private void DrawTracks(Rect map, YardMiniMapBuilder.Snapshot snap)
    {
        for (var i = 0; i < snap.Polylines.Count; i++)
        {
            var poly = snap.Polylines[i];
            if (poly == null || poly.Length < 2)
            {
                continue;
            }

            for (var j = 1; j < poly.Length; j++)
            {
                if (!TryProject(map, snap, poly[j - 1].X, poly[j - 1].Z, out var x0, out var y0))
                {
                    continue;
                }

                if (!TryProject(map, snap, poly[j].X, poly[j].Z, out var x1, out var y1))
                {
                    continue;
                }

                DrawLine(x0, y0, x1, y1, TrackColor, 1.5f);
            }
        }
    }

    private void DrawLandmarks(Rect map, YardMiniMapBuilder.Snapshot snap)
    {
        if (snap.HasOffice
            && TryProject(map, snap, snap.OfficeX, snap.OfficeZ, out var ox, out var oy))
        {
            DrawDot(ox, oy, LandmarkRadius, OfficeColor);
            GUI.Label(new Rect(ox + 6f, oy - 8f, 48f, 16f), "Office", _labelStyle);
        }

        for (var i = 0; i < snap.Turntables.Count; i++)
        {
            var tt = snap.Turntables[i];
            if (!TryProject(map, snap, tt.X, tt.Z, out var tx, out var ty))
            {
                continue;
            }

            DrawDot(tx, ty, LandmarkRadius, TtColor);
            GUI.Label(new Rect(tx + 6f, ty - 8f, 28f, 16f), "TT", _labelStyle);
        }
    }

    private void DrawPlayer(Rect map, YardMiniMapBuilder.Snapshot snap)
    {
        if (!TelemetryReader.TryGetPlayerPosition(out var px, out _, out var pz))
        {
            return;
        }

        if (YardMiniMapProjection.TryOffMapEdge(
                px,
                pz,
                snap.MinX,
                snap.MaxX,
                snap.MinZ,
                snap.MaxZ,
                map.x,
                map.y,
                map.width,
                map.height,
                PinClampInset,
                out var edgeX,
                out var edgeY,
                out var dirX,
                out var dirY))
        {
            DrawOffMapArrow(edgeX, edgeY, dirX, dirY, PinColor);
            return;
        }

        if (!TryProject(map, snap, px, pz, out var x, out var y))
        {
            return;
        }

        DrawDot(x, y, PinRadius, PinColor);

        var heading = TelemetryReader.TryGetHeadingDegrees();
        if (heading is float deg)
        {
            YardMiniMapProjection.HeadingTickOffset(deg, HeadingTickLength, out var dx, out var dy);
            DrawLine(x, y, x + dx, y + dy, PinColor, 2f);
        }
    }

    private void DrawOffMapArrow(float edgeX, float edgeY, float dirX, float dirY, Color color)
    {
        // Tip points outward (toward off-map player); base sits on the border.
        var tipX = edgeX + dirX * OffMapArrowSize;
        var tipY = edgeY + dirY * OffMapArrowSize;
        var bx = -dirY * (OffMapArrowSize * 0.55f);
        var by = dirX * (OffMapArrowSize * 0.55f);
        var leftX = edgeX - dirX * 2f + bx;
        var leftY = edgeY - dirY * 2f + by;
        var rightX = edgeX - dirX * 2f - bx;
        var rightY = edgeY - dirY * 2f - by;
        DrawLine(tipX, tipY, leftX, leftY, color, 2.5f);
        DrawLine(tipX, tipY, rightX, rightY, color, 2.5f);
        DrawLine(leftX, leftY, rightX, rightY, color, 2f);
    }

    private static bool TryProject(
        Rect map,
        YardMiniMapBuilder.Snapshot snap,
        float worldX,
        float worldZ,
        out float panelX,
        out float panelY) =>
        YardMiniMapProjection.TryWorldToPanel(
            worldX,
            worldZ,
            snap.MinX,
            snap.MaxX,
            snap.MinZ,
            snap.MaxZ,
            map.x,
            map.y,
            map.width,
            map.height,
            out panelX,
            out panelY);

    private void EnsureStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.92f, 0.95f, 1f) },
        };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.85f, 0.88f, 0.9f, 1f) },
        };
    }

    private void EnsurePixel()
    {
        if (_pixel != null)
        {
            return;
        }

        _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
        };
        _pixel.SetPixel(0, 0, Color.white);
        _pixel.Apply(false, true);
    }

    private void DrawFill(Rect rect, Color color)
    {
        EnsurePixel();
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, _pixel);
        GUI.color = prev;
    }

    private void DrawDot(float cx, float cy, float radius, Color color)
    {
        DrawFill(new Rect(cx - radius, cy - radius, radius * 2f, radius * 2f), color);
    }

    private void DrawLine(float x0, float y0, float x1, float y1, Color color, float thickness)
    {
        EnsurePixel();
        var dx = x1 - x0;
        var dy = y1 - y0;
        var len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f)
        {
            return;
        }

        var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        var prev = GUI.color;
        GUI.color = color;
        var matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x0, y0));
        GUI.DrawTexture(new Rect(x0, y0 - thickness * 0.5f, len, thickness), _pixel);
        GUI.matrix = matrix;
        GUI.color = prev;
    }
}
