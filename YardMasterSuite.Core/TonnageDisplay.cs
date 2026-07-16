using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure tonnage formatting. Game mass is kilograms; display metric tonnes.
/// </summary>
public static class TonnageDisplay
{
    public const float KilogramsPerTonne = 1000f;

    public static float KilogramsToTonnes(float kilograms) =>
        kilograms / KilogramsPerTonne;

    public static string FormatTonnes(float? tonnes) =>
        tonnes is null
            ? "— t"
            : $"{Math.Round(tonnes.Value, MidpointRounding.AwayFromZero):0} t";

    public static string FormatFromKilograms(float? kilograms) =>
        kilograms is null
            ? "— t"
            : FormatTonnes(KilogramsToTonnes(kilograms.Value));
}
