using System;
using System.IO;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// AR screen-space wayfinding markers (4.9): loco / station office / pin icons.
/// PNG shapes are primary; tint colors are secondary. Glyph text is fallback only.
/// </summary>
public sealed class ArWaypointOverlay : MonoBehaviour
{
    private const float IconPixels = 48f;
    private const float LabelFontSize = 14f;
    private const float VerticalLiftMeters = 3.5f;

    private static readonly Color LocoColor = new(0.31f, 0.76f, 0.97f, 1f);
    private static readonly Color StationColor = new(0.51f, 0.78f, 0.52f, 1f);
    private static readonly Color PinColor = new(1f, 0.84f, 0.31f, 1f);

    private GUIStyle? _labelStyle;
    private Texture2D? _locoIcon;
    private Texture2D? _stationIcon;
    private Texture2D? _pinIcon;
    private bool _iconsLoadAttempted;

    private bool _hasArDebug;
    private bool _lastLoco;
    private bool _lastStation;
    private bool _lastPin;

    private void OnDestroy()
    {
        DestroyTexture(ref _locoIcon);
        DestroyTexture(ref _stationIcon);
        DestroyTexture(ref _pinIcon);
    }

    private void OnGUI()
    {
        if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null))
        {
            EmitArDebug(false, false, false);
            return;
        }

        EnsureStyles();
        EnsureIcons();
        var cam = PlayerManager.ActiveCamera;
        if (cam == null)
        {
            EmitArDebug(false, false, false);
            return;
        }

        var playerPos = PlayerManager.PlayerTransform != null
            ? PlayerManager.PlayerTransform.position
            : cam.transform.position;

        var locoOn = TryDrawMarker(
            cam,
            playerPos,
            ArWaypointKind.Loco,
            TelemetryReader.TryGetArLocoWorldPosition,
            LocoColor,
            _locoIcon);
        var stationOn = TryDrawMarker(
            cam,
            playerPos,
            ArWaypointKind.Station,
            TelemetryReader.TryGetArStationOfficeWorldPosition,
            StationColor,
            _stationIcon);
        var pinOn = TryDrawMarker(
            cam,
            playerPos,
            ArWaypointKind.Pin,
            TelemetryReader.TryGetArPinWorldPosition,
            PinColor,
            _pinIcon);
        EmitArDebug(locoOn, stationOn, pinOn);
    }

    private bool TryDrawMarker(
        Camera cam,
        Vector3 playerPos,
        ArWaypointKind kind,
        TryGetWorldPosition getter,
        Color color,
        Texture2D? icon)
    {
        if (!getter(out var world))
        {
            return false;
        }

        var lifted = world + Vector3.up * VerticalLiftMeters;
        var toTarget = lifted - cam.transform.position;
        var viewForward = Vector3.Dot(toTarget, cam.transform.forward);
        var viewRight = Vector3.Dot(toTarget, cam.transform.right);
        var viewUp = Vector3.Dot(toTarget, cam.transform.up);
        var behind = ArMarkerProjection.IsBehindCamera(viewForward);

        // Ahead: Unity projection X. Behind: view-plane atan2 → edge X (A.1).
        // Sticky row (A.2): pin Y under HUD stack — no pitch wander.
        var screen = cam.WorldToScreenPoint(lifted);
        var x = screen.x;
        var y = screen.y;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behind,
            viewRight,
            viewUp,
            Screen.width,
            Screen.height,
            ArMarkerProjection.DefaultEdgeMarginPixels,
            ref x,
            ref y);

        var dx = world.x - playerPos.x;
        var dy = world.y - playerPos.y;
        var dz = world.z - playerPos.z;
        var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        var distLabel = ArMarkerDisplay.FormatDistanceOnly(dist);
        var labelSize = string.IsNullOrEmpty(distLabel)
            ? Vector2.zero
            : _labelStyle!.CalcSize(new GUIContent(distLabel));
        var iconH = icon != null ? IconPixels : 22f;
        var iconW = icon != null ? IconPixels : 22f;
        var width = Mathf.Max(iconW, labelSize.x) + 8f;
        var height = iconH + (labelSize.y > 0 ? labelSize.y + 2f : 0f);

        var stackBottom = MonitorHudDriver.LastStackBottomGuiY;
        if (stackBottom < 1f)
        {
            stackBottom = MonitorHudStackLayout.StackBottomGuiY(false, false, false);
        }

        var stickyTop = MonitorHudStackLayout.StickyRowTopGuiY(stackBottom);
        var stickyCenterGuiY = stickyTop + height * 0.5f;
        ArStickyRowPlacement.PinScreenYToStickyRow(stickyCenterGuiY, Screen.height, ref y);

        var clamped = ArMarkerProjection.ClampToScreen(
            x,
            y,
            Screen.width,
            Screen.height,
            ArMarkerProjection.DefaultEdgeMarginPixels,
            out x,
            out y);

        var guiY = ArStickyRowPlacement.MarkerTopGuiY(stickyTop, height);
        var rect = new Rect(x - width * 0.5f, guiY, width, height);

        var plate = new Rect(rect.x - 4f, rect.y - 2f, rect.width + 8f, rect.height + 4f);
        var plateColor = new Color(0.08f, 0.08f, 0.08f, clamped || behind ? 0.72f : 0.55f);
        DrawQuad(plate, plateColor);

        var prev = GUI.color;
        GUI.color = color;
        var iconRect = new Rect(rect.x + (width - iconW) * 0.5f, rect.y, iconW, iconH);
        if (icon != null)
        {
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
        }
        else
        {
            // Fallback if Icons/*.png failed to load.
            var glyph = ArMarkerDisplay.Glyph(kind);
            GUI.Label(iconRect, glyph, _labelStyle);
        }

        if (!string.IsNullOrEmpty(distLabel))
        {
            GUI.color = Color.white;
            GUI.Label(
                new Rect(rect.x, rect.y + iconH, rect.width, labelSize.y),
                distLabel,
                _labelStyle);
        }

        GUI.color = prev;
        return true;
    }

    private void EmitArDebug(bool loco, bool station, bool pin)
    {
        var snap = new ArWaypointDebugSnapshot(loco, station, pin);
        ArWaypointDebugSnapshot? previous = null;
        if (_hasArDebug)
        {
            previous = new ArWaypointDebugSnapshot(_lastLoco, _lastStation, _lastPin);
        }

        var line = Tier2ArWaypointDebug.NextLogMessage(previous, snap);
        _lastLoco = loco;
        _lastStation = station;
        _lastPin = pin;
        _hasArDebug = true;
        if (line != null)
        {
            Main.Log(line);
        }
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null)
        {
            return;
        }

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = (int)LabelFontSize,
            fontStyle = FontStyle.Bold,
            richText = false,
            normal = { textColor = Color.white },
        };
    }

    private void EnsureIcons()
    {
        if (_iconsLoadAttempted)
        {
            return;
        }

        _iconsLoadAttempted = true;
        _locoIcon = TryLoadIcon("loco.png");
        _stationIcon = TryLoadIcon("station.png");
        _pinIcon = TryLoadIcon("pin.png");
    }

    private static Texture2D? TryLoadIcon(string fileName)
    {
        try
        {
            var path = Main.IconsPath;
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var full = Path.Combine(path, fileName);
            if (!File.Exists(full))
            {
                Main.Log($"AR icon missing: {full}");
                return null;
            }

            var bytes = File.ReadAllBytes(full);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!tex.LoadImage(bytes))
            {
                DestroyTexture(ref tex);
                Main.Log($"AR icon LoadImage failed: {fileName}");
                return null;
            }

            return tex;
        }
        catch (Exception ex)
        {
            Main.Log($"AR icon load error ({fileName}): {ex.Message}");
            return null;
        }
    }

    private static void DrawQuad(Rect rect, Color color)
    {
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private static void DestroyTexture(ref Texture2D? tex)
    {
        if (tex == null)
        {
            return;
        }

        UnityEngine.Object.Destroy(tex);
        tex = null;
    }

    private delegate bool TryGetWorldPosition(out Vector3 world);
}
