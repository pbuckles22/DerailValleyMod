using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Square mini-map panel size (4.13). Fixed px with screen clamp — UMM resize parked end-of-story.
/// </summary>
public static class YardMiniMapPanelLayout
{
    /// <summary>Smoke target after 0.6.31 still-too-small FAIL (560→1120).</summary>
    public const float DefaultPanelSizePixels = 1120f;

    public const float MinPanelSizePixels = 160f;

    /// <summary>
    /// Square edge in pixels: <paramref name="requestedSize"/> clamped to fit
    /// <c>min(screenW, screenH) - 2*margin</c>.
    /// </summary>
    public static float ResolveSquarePanelSize(
        float screenWidth,
        float screenHeight,
        float marginPixels,
        float requestedSize = DefaultPanelSizePixels)
    {
        var margin = marginPixels < 0f ? 0f : marginPixels;
        var maxByScreen = Math.Min(screenWidth, screenHeight) - 2f * margin;
        if (maxByScreen < MinPanelSizePixels)
        {
            return maxByScreen > 1f ? maxByScreen : 1f;
        }

        var size = requestedSize < MinPanelSizePixels ? MinPanelSizePixels : requestedSize;
        return size > maxByScreen ? maxByScreen : size;
    }
}
