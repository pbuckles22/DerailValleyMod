namespace YardMasterSuite.Core;

/// <summary>
/// Pure next-station formatting for the loco HUD when fluids are low (4.5).
/// Null means omit from the train bar join.
/// </summary>
public static class NextStationDisplay
{
    /// <summary>
    /// Fluid-gated next-station chip, or null when fluids are OK / station path unknown.
    /// </summary>
    public static string? Format(bool fluidsLow, string? stationLabel, float? distanceMeters)
    {
        if (!fluidsLow || distanceMeters is null)
        {
            return null;
        }

        var label = stationLabel?.Trim();
        if (string.IsNullOrEmpty(label))
        {
            return null;
        }

        var km = distanceMeters.Value / 1000f;
        return $"Next: {label} [{km.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} km]";
    }

    /// <summary>True when either fluid is at/below the yellow warn band (includes critical).</summary>
    public static bool FluidsLow(float? fuelPercent, float? oilPercent) =>
        FluidDisplay.IsLow(fuelPercent) || FluidDisplay.IsLow(oilPercent);
}
