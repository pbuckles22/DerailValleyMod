namespace YardMasterSuite.Core;

/// <summary>
/// Shared Monitor HUD stack geometry (GUI / top-left origin) for bars + AR sticky row (Bundle A.2).
/// Must stay in sync with <c>MonitorHudDriver</c> draw constants.
/// </summary>
public static class MonitorHudStackLayout
{
    public const float Pad = 12f;
    public const float BarHeight = 28f;
    public const float Gap = 4f;
    public const float StickyRowGap = 8f;

    /// <summary>
    /// GUI Y just below the last bar (always-on is always drawn in-world).
    /// Stack order: loco → look-at → job → always-on.
    /// </summary>
    public static float StackBottomGuiY(bool hasTrainBar, bool hasLocalBar, bool hasJobBar)
    {
        var y = Pad;
        if (hasTrainBar)
        {
            y += BarHeight + Gap;
        }

        if (hasLocalBar)
        {
            y += BarHeight + Gap;
        }

        if (hasJobBar)
        {
            y += BarHeight + Gap;
        }

        return y + BarHeight;
    }

    /// <summary>Top of the AR sticky marker row (GUI Y, top-left origin).</summary>
    public static float StickyRowTopGuiY(float stackBottomGuiY, float gapBelowHud = StickyRowGap) =>
        stackBottomGuiY + gapBelowHud;
}

/// <summary>
/// Sticky-row placement: keep turn-cue X from projection / edge; pin Y under the HUD stack.
/// Screen coords use Unity bottom-left origin unless noted as GUI.
/// </summary>
public static class ArStickyRowPlacement
{
    /// <summary>
    /// Overwrite <paramref name="screenY"/> so the marker sits on the sticky row.
    /// <paramref name="stickyRowCenterGuiY"/> is the GUI (top-origin) Y of the icon center.
    /// </summary>
    public static void PinScreenYToStickyRow(
        float stickyRowCenterGuiY,
        float screenHeight,
        ref float screenY) =>
        screenY = screenHeight - stickyRowCenterGuiY;

    /// <summary>
    /// GUI Y for the top of a marker whose vertical center sits on the sticky strip.
    /// </summary>
    public static float MarkerTopGuiY(float stickyRowTopGuiY, float markerHeight) =>
        stickyRowTopGuiY;
}
