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
    /// <summary>Park pin: sit near stand height (no loco/office lift).</summary>
    private const float PinVerticalLiftMeters = 0.6f;

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

    private MarkerMotion _locoMotion;
    private MarkerMotion _stationMotion;
    private MarkerMotion _pinMotion;

    private readonly MarkerFrame[] _frames = new MarkerFrame[3];

    private struct MarkerMotion
    {
        public ArHorizontalEdge Edge;
        public bool WasBehind;
        public float Progress;
        public float FromX;
        public float FromGuiY;
        public bool WantedOnObject;
        public float LastObjectX;
        public float LastObjectGuiY;
        public bool HasObjectAnchor;
        public float LastDrawX;
        public float LastDrawGuiY;
        public bool HasLastDraw;
    }

    private struct MarkerFrame
    {
        public ArWaypointKind Kind;
        public Color Color;
        public Texture2D? Icon;
        public string DistLabel;
        public bool Behind;
        public float BehindBearing;
        public float StickyX;
        public float StickyGuiCenter;
        public bool WantOnObject;
        public MarkerMotion Motion;
    }

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
        var dt = Time.unscaledDeltaTime;

        var n = 0;
        if (TryPrepareMarker(
                cam,
                playerPos,
                ArWaypointKind.Loco,
                TelemetryReader.TryGetArLocoWorldPosition,
                LocoColor,
                _locoIcon,
                ref _locoMotion,
                out _frames[n]))
        {
            n++;
        }

        if (TryPrepareMarker(
                cam,
                playerPos,
                ArWaypointKind.Station,
                TelemetryReader.TryGetArStationOfficeWorldPosition,
                StationColor,
                _stationIcon,
                ref _stationMotion,
                out _frames[n]))
        {
            n++;
        }

        if (TryPrepareMarker(
                cam,
                playerPos,
                ArWaypointKind.Pin,
                TelemetryReader.TryGetArPinWorldPosition,
                PinColor,
                _pinIcon,
                ref _pinMotion,
                out _frames[n]))
        {
            n++;
        }

        ApplyEdgeStackOffsets(_frames, n);

        var locoOn = false;
        var stationOn = false;
        var pinOn = false;
        for (var i = 0; i < n; i++)
        {
            FinishMarkerMotionAndDraw(ref _frames[i], dt);
            switch (_frames[i].Kind)
            {
                case ArWaypointKind.Loco:
                    locoOn = true;
                    break;
                case ArWaypointKind.Station:
                    stationOn = true;
                    break;
                case ArWaypointKind.Pin:
                    pinOn = true;
                    break;
            }
        }

        EmitArDebug(locoOn, stationOn, pinOn);
    }

    private bool TryPrepareMarker(
        Camera cam,
        Vector3 playerPos,
        ArWaypointKind kind,
        TryGetWorldPosition getter,
        Color color,
        Texture2D? icon,
        ref MarkerMotion motion,
        out MarkerFrame frame)
    {
        frame = default;
        if (!getter(out var world))
        {
            motion.Progress = 0f;
            motion.HasObjectAnchor = false;
            motion.HasLastDraw = false;
            motion.WantedOnObject = false;
            return false;
        }

        var lift = kind == ArWaypointKind.Pin ? PinVerticalLiftMeters : VerticalLiftMeters;
        var lifted = world + Vector3.up * lift;
        var toTarget = lifted - cam.transform.position;
        var viewForward = Vector3.Dot(toTarget, cam.transform.forward);
        var viewRight = Vector3.Dot(toTarget, cam.transform.right);
        var behind = ArMarkerProjection.IsBehindCameraHysteresis(viewForward, motion.WasBehind);
        motion.WasBehind = behind;

        var screen = cam.WorldToScreenPoint(lifted);
        var dx = world.x - playerPos.x;
        var dy = world.y - playerPos.y;
        var dz = world.z - playerPos.z;
        var dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        var distLabel = ArMarkerDisplay.FormatDistanceOnly(dist);

        var labelSize = string.IsNullOrEmpty(distLabel)
            ? Vector2.zero
            : _labelStyle!.CalcSize(new GUIContent(distLabel));
        var iconH = icon != null ? IconPixels : 22f;
        var height = iconH + (labelSize.y > 0 ? labelSize.y + 2f : 0f);

        var stackBottom = MonitorHudDriver.LastStackBottomGuiY;
        if (stackBottom < 1f)
        {
            stackBottom = MonitorHudStackLayout.StackBottomGuiY(false, false, false);
        }

        var stickyTop = MonitorHudStackLayout.StickyRowTopGuiY(stackBottom);
        var stickyX = screen.x;
        var stickyScreenY = screen.y;
        var bearing = ArEdgeHysteresis.BehindBearingRadians(viewRight, viewForward);
        if (behind)
        {
            motion.Edge = ArEdgeHysteresis.Resolve(viewRight, viewForward, motion.Edge);
            ArMarkerProjection.ApplyBehindCameraHorizontalEdge(
                true,
                motion.Edge,
                Screen.width,
                Screen.height,
                ArMarkerProjection.DefaultEdgeMarginPixels,
                ref stickyX,
                ref stickyScreenY);
        }
        else
        {
            motion.Edge = ArHorizontalEdge.None;
        }

        var stickyCenterGuiY = stickyTop + height * 0.5f;
        ArStickyRowPlacement.PinScreenYToStickyRow(stickyCenterGuiY, Screen.height, ref stickyScreenY);
        ArMarkerProjection.ClampToScreen(
            stickyX,
            stickyScreenY,
            Screen.width,
            Screen.height,
            ArMarkerProjection.DefaultEdgeMarginPixels,
            out stickyX,
            out stickyScreenY);
        var stickyGuiCenter = ArStickyRowPlacement.MarkerTopGuiY(stickyTop, height) + height * 0.5f;

        var wantOnObject = ArMarkerProjection.ShouldPlaceOnObject(
            behind,
            screen.z,
            screen.x,
            screen.y,
            Screen.width,
            Screen.height);
        if (wantOnObject || (!behind && screen.z > 0.05f))
        {
            motion.LastObjectX = screen.x;
            motion.LastObjectGuiY = ArMarkerProjection.ToGuiY(screen.y, Screen.height);
            motion.HasObjectAnchor = true;
        }

        if (!motion.HasObjectAnchor && wantOnObject)
        {
            wantOnObject = false;
        }

        frame = new MarkerFrame
        {
            Kind = kind,
            Color = color,
            Icon = icon,
            DistLabel = distLabel,
            Behind = behind,
            BehindBearing = bearing,
            StickyX = stickyX,
            StickyGuiCenter = stickyGuiCenter,
            WantOnObject = wantOnObject,
            Motion = motion,
        };
        return true;
    }

    private static void ApplyEdgeStackOffsets(MarkerFrame[] frames, int n)
    {
        if (n < 2)
        {
            return;
        }

        ApplyEdgeStackForSide(frames, n, ArHorizontalEdge.Left);
        ApplyEdgeStackForSide(frames, n, ArHorizontalEdge.Right);
    }

    private static void ApplyEdgeStackForSide(MarkerFrame[] frames, int n, ArHorizontalEdge side)
    {
        var margin = ArMarkerProjection.DefaultEdgeMarginPixels;
        var outermost = side == ArHorizontalEdge.Left
            ? margin
            : Math.Max(margin, Screen.width - margin);

        var indices = new int[3];
        var count = 0;
        for (var i = 0; i < n; i++)
        {
            ref var f = ref frames[i];
            if (f.WantOnObject)
            {
                continue;
            }

            var edge = f.Behind
                ? f.Motion.Edge
                : ArEdgeStackLayout.DetectEdge(f.StickyX, Screen.width, margin);
            if (edge != side)
            {
                continue;
            }

            indices[count++] = i;
        }

        if (count < 2)
        {
            return;
        }

        var keys = new float[count];
        var xs = new float[count];
        for (var i = 0; i < count; i++)
        {
            keys[i] = ArEdgeStackLayout.OutwardSortKey(side, frames[indices[i]].BehindBearing);
        }

        ArEdgeStackLayout.AssignStackedXs(side, outermost, ArEdgeStackLayout.DefaultSeparationPixels, keys, xs);
        for (var i = 0; i < count; i++)
        {
            frames[indices[i]].StickyX = xs[i];
        }
    }

    private void FinishMarkerMotionAndDraw(ref MarkerFrame frame, float deltaSeconds)
    {
        var motion = frame.Motion;
        var stickyX = frame.StickyX;
        var stickyGuiCenter = frame.StickyGuiCenter;
        var wantOnObject = frame.WantOnObject;

        if (wantOnObject != motion.WantedOnObject)
        {
            if (motion.HasLastDraw)
            {
                motion.FromX = motion.LastDrawX;
                motion.FromGuiY = motion.LastDrawGuiY;
            }
            else if (motion.WantedOnObject && motion.HasObjectAnchor)
            {
                motion.FromX = motion.LastObjectX;
                motion.FromGuiY = motion.LastObjectGuiY;
            }
            else
            {
                motion.FromX = stickyX;
                motion.FromGuiY = stickyGuiCenter;
            }

            motion.Progress = 0f;
            motion.WantedOnObject = wantOnObject;
        }

        motion.Progress = ArMarkerTransition.StepProgress(motion.Progress, deltaSeconds);

        var targetX = motion.WantedOnObject && motion.HasObjectAnchor ? motion.LastObjectX : stickyX;
        var targetGuiY = motion.WantedOnObject && motion.HasObjectAnchor
            ? motion.LastObjectGuiY
            : stickyGuiCenter;

        ArMarkerTransition.Lerp(
            motion.FromX,
            motion.FromGuiY,
            targetX,
            targetGuiY,
            motion.Progress,
            out var drawX,
            out var drawGuiCenter);

        motion.LastDrawX = drawX;
        motion.LastDrawGuiY = drawGuiCenter;
        motion.HasLastDraw = true;

        switch (frame.Kind)
        {
            case ArWaypointKind.Loco:
                _locoMotion = motion;
                break;
            case ArWaypointKind.Station:
                _stationMotion = motion;
                break;
            case ArWaypointKind.Pin:
                _pinMotion = motion;
                break;
        }

        var emphasize = motion.Progress < 0.5f
            && !motion.WantedOnObject
            && (frame.Behind || Mathf.Abs(stickyX - Screen.width * 0.5f) > Screen.width * 0.35f);
        DrawMarkerVisual(
            drawX,
            drawGuiCenter,
            frame.Kind,
            frame.Color,
            frame.Icon,
            frame.DistLabel,
            guiYIsCenter: true,
            emphasize);
    }

    private void DrawMarkerVisual(
        float x,
        float guiY,
        ArWaypointKind kind,
        Color color,
        Texture2D? icon,
        string? distLabel,
        bool guiYIsCenter,
        bool emphasize)
    {
        var labelSize = string.IsNullOrEmpty(distLabel)
            ? Vector2.zero
            : _labelStyle!.CalcSize(new GUIContent(distLabel!));
        var iconH = icon != null ? IconPixels : 22f;
        var iconW = icon != null ? IconPixels : 22f;

        var width = Mathf.Max(iconW, labelSize.x) + 8f;
        var height = iconH + (labelSize.y > 0 ? labelSize.y + 2f : 0f);
        var left = x - width * 0.5f;
        var top = guiYIsCenter ? guiY - height * 0.5f : guiY;
        var rect = new Rect(left, top, width, height);

        var plate = new Rect(rect.x - 4f, rect.y - 2f, rect.width + 8f, rect.height + 4f);
        var plateAlpha = emphasize ? 0.72f : 0.55f;
        DrawQuad(plate, new Color(0.08f, 0.08f, 0.08f, plateAlpha));

        var prev = GUI.color;
        GUI.color = color;
        var iconRect = new Rect(rect.x + (width - iconW) * 0.5f, rect.y, iconW, iconH);
        if (icon != null)
        {
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
        }
        else
        {
            GUI.Label(iconRect, ArMarkerDisplay.Glyph(kind), _labelStyle);
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
