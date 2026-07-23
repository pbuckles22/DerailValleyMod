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

    /// <summary>
    /// When the target is behind the camera, flip to the opposite screen side so edge clamp
    /// points the correct way to turn.
    /// </summary>
    public static void ApplyBehindCameraFlip(
        bool behindCamera,
        float screenWidth,
        float screenHeight,
        ref float screenX,
        ref float screenY)
    {
        if (!behindCamera)
        {
            return;
        }

        screenX = screenWidth - screenX;
        screenY = screenHeight - screenY;
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
