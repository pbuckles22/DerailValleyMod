using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Waypoint kinds for AR screen markers (4.9). Shape glyph is primary; color is secondary.
/// </summary>
public enum ArWaypointKind
{
    Loco = 0,
    Station = 1,
    Pin = 2,
}

/// <summary>
/// Pure screen helpers for AR markers (Unity supplies <c>WorldToScreenPoint</c>).
/// Screen coords use Unity’s bottom-left origin.
/// </summary>
public static class ArMarkerProjection
{
    public const float DefaultEdgeMarginPixels = 28f;

    /// <summary>Camera-forward rejection: target is behind / on the near plane.</summary>
    public static bool IsBehindCamera(float viewForward, float epsilon = 0.05f) =>
        viewForward <= epsilon;

    /// <summary>
    /// Behind-state with hysteresis so glancing the camera plane does not flicker ahead/behind.
    /// </summary>
    public static bool IsBehindCameraHysteresis(
        float viewForward,
        bool wasBehind,
        float enterBehind = 0.05f,
        float exitAhead = 0.35f)
    {
        if (wasBehind)
        {
            return viewForward <= exitAhead;
        }

        return viewForward <= enterBehind;
    }

    /// <summary>True when a Unity screen point is inside the pixel rect (optional inflate).</summary>
    public static bool IsOnScreen(
        float screenX,
        float screenY,
        float screenWidth,
        float screenHeight,
        float inflatePixels = 0f) =>
        screenX >= -inflatePixels
        && screenX <= screenWidth + inflatePixels
        && screenY >= -inflatePixels
        && screenY <= screenHeight + inflatePixels;

    /// <summary>Ahead + on-screen → place marker on the object (not sticky row). Mutual exclusive with sticky.</summary>
    public static bool ShouldPlaceOnObject(
        bool behindCamera,
        float screenZ,
        float screenX,
        float screenY,
        float screenWidth,
        float screenHeight) =>
        !behindCamera
        && screenZ > 0.05f
        && IsOnScreen(screenX, screenY, screenWidth, screenHeight);

    /// <summary>
    /// When behind the camera, replace unreliable <c>WorldToScreenPoint</c> coords with a
    /// screen-edge point from atan2(viewRight, viewUp) so clamp becomes a turn cue — never fake center.
    /// When ahead, leaves <paramref name="screenX"/> / <paramref name="screenY"/> unchanged.
    /// Prefer <see cref="ApplyBehindCameraHorizontalEdge"/> for sticky-row turn cues (no L/R stutter).
    /// </summary>
    public static void ApplyBehindCameraEdge(
        bool behindCamera,
        float viewRight,
        float viewUp,
        float screenWidth,
        float screenHeight,
        float edgeMargin,
        ref float screenX,
        ref float screenY)
    {
        if (!behindCamera)
        {
            return;
        }

        ProjectViewDirectionToEdge(
            viewRight,
            viewUp,
            screenWidth,
            screenHeight,
            edgeMargin,
            out screenX,
            out screenY);
    }

    /// <summary>
    /// Behind-camera sticky turn cue: left or right edge only (ignores viewUp — no center yank / L-R stutter).
    /// </summary>
    public static void ApplyBehindCameraHorizontalEdge(
        bool behindCamera,
        ArHorizontalEdge side,
        float screenWidth,
        float screenHeight,
        float edgeMargin,
        ref float screenX,
        ref float screenY)
    {
        if (!behindCamera)
        {
            return;
        }

        var minX = edgeMargin;
        var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
        screenX = side == ArHorizontalEdge.Right ? maxX : minX;
        screenY = screenHeight * 0.5f;
    }

    /// <summary>
    /// Map view-plane lateral offsets to the inset screen rectangle edge (Unity bottom-left origin).
    /// </summary>
    public static void ProjectViewDirectionToEdge(
        float viewRight,
        float viewUp,
        float screenWidth,
        float screenHeight,
        float edgeMargin,
        out float screenX,
        out float screenY)
    {
        var minX = edgeMargin;
        var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
        var minY = edgeMargin;
        var maxY = Math.Max(edgeMargin, screenHeight - edgeMargin);
        var cx = (minX + maxX) * 0.5f;
        var cy = (minY + maxY) * 0.5f;

        // Degenerate: no lateral signal — default bottom-center (turn-around cue).
        if (Math.Abs(viewRight) < 1e-6f && Math.Abs(viewUp) < 1e-6f)
        {
            screenX = cx;
            screenY = minY;
            return;
        }

        // 0 = up in view, +π/2 = right (Unity screen +X right, +Y up).
        var angle = Math.Atan2(viewRight, viewUp);
        var dx = (float)Math.Sin(angle);
        var dy = (float)Math.Cos(angle);

        var t = float.PositiveInfinity;
        if (dx > 1e-6f)
        {
            t = Math.Min(t, (maxX - cx) / dx);
        }
        else if (dx < -1e-6f)
        {
            t = Math.Min(t, (minX - cx) / dx);
        }

        if (dy > 1e-6f)
        {
            t = Math.Min(t, (maxY - cy) / dy);
        }
        else if (dy < -1e-6f)
        {
            t = Math.Min(t, (minY - cy) / dy);
        }

        if (float.IsInfinity(t) || t < 0f)
        {
            screenX = cx;
            screenY = minY;
            return;
        }

        screenX = cx + dx * t;
        screenY = cy + dy * t;
    }

    /// <summary>
    /// Clamp a screen point into the safe rect. Returns true if moved.
    /// </summary>
    public static bool ClampToScreen(
        float x,
        float y,
        float screenWidth,
        float screenHeight,
        float edgeMargin,
        out float clampedX,
        out float clampedY)
    {
        var minX = edgeMargin;
        var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
        var minY = edgeMargin;
        var maxY = Math.Max(edgeMargin, screenHeight - edgeMargin);

        clampedX = Math.Min(maxX, Math.Max(minX, x));
        clampedY = Math.Min(maxY, Math.Max(minY, y));
        return Math.Abs(clampedX - x) > 0.5f || Math.Abs(clampedY - y) > 0.5f;
    }

    /// <summary>GUI Y (top-left origin) from Unity screen Y (bottom-left origin).</summary>
    public static float ToGuiY(float screenYBottomOrigin, float screenHeight) =>
        screenHeight - screenYBottomOrigin;
}

/// <summary>Pure AR marker label formatting (distinct glyph + distance).</summary>
public static class ArMarkerDisplay
{
    public static string FormatLabel(ArWaypointKind kind, float? distanceMeters)
    {
        var glyph = Glyph(kind);
        if (distanceMeters is null)
        {
            return glyph;
        }

        var meters = (int)Math.Round(Math.Max(0f, distanceMeters.Value), MidpointRounding.AwayFromZero);
        return $"{glyph} {meters}m";
    }

    /// <summary>Distance-only caption under a texture icon.</summary>
    public static string FormatDistanceOnly(float? distanceMeters)
    {
        if (distanceMeters is null)
        {
            return "";
        }

        var meters = (int)Math.Round(Math.Max(0f, distanceMeters.Value), MidpointRounding.AwayFromZero);
        return $"{meters}m";
    }

    /// <summary>Distinct shapes (not color-only): loco ▲, station ⌂, pin ●.</summary>
    public static string Glyph(ArWaypointKind kind) =>
        kind switch
        {
            ArWaypointKind.Loco => "▲",
            ArWaypointKind.Station => "⌂",
            ArWaypointKind.Pin => "●",
            _ => "•",
        };
}
