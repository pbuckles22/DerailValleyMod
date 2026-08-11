using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Switch-list pin prompt copy. Pin is always the switch (not the safe-zone rim).
/// </summary>
public static class SwitchListClearPrompt
{
    public const string ClearedCaption = "CLEARED · Next";

    /// <summary>
    /// Inside / near the switch: meters left to the clear rim.
    /// </summary>
    public static string FormatAtSwitch(int metersToClearRim)
    {
        if (metersToClearRim <= 0)
        {
            return "At switch · keep going";
        }

        return "At switch · keep going · " + metersToClearRim + "m";
    }

    /// <summary>
    /// True when the ref is inside the safe radius (hit the pin / danger zone).
    /// </summary>
    public static bool IsInsideDangerCircle(float distToPinMeters, float safeRadiusMeters) =>
        safeRadiusMeters > 0f
        && !float.IsNaN(safeRadiusMeters)
        && distToPinMeters >= 0f
        && !float.IsNaN(distToPinMeters)
        && distToPinMeters < safeRadiusMeters;
}
