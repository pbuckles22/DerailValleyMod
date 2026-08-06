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
            ? "— Mass"
            : $"Mass {Math.Round(tonnes.Value, MidpointRounding.AwayFromZero):0} t";

    public static string FormatFromKilograms(float? kilograms) =>
        kilograms is null
            ? "— Mass"
            : FormatTonnes(KilogramsToTonnes(kilograms.Value));

    /// <summary>
    /// Look-at / standing: this car's mass; when coupled to others, also total trainset mass.
    /// </summary>
    public static string FormatCarAndConsistFromKilograms(float? carKilograms, float? consistKilograms)
    {
        if (carKilograms is null)
        {
            return "— Car";
        }

        var carTonnes = Math.Round(
            KilogramsToTonnes(carKilograms.Value),
            MidpointRounding.AwayFromZero);
        var carChip = $"Car {carTonnes:0} t";

        if (consistKilograms is null)
        {
            return carChip;
        }

        // Solo / same mass — no all-cars chip.
        if (consistKilograms.Value <= carKilograms.Value * 1.01f + 1f)
        {
            return carChip;
        }

        var consistTonnes = Math.Round(
            KilogramsToTonnes(consistKilograms.Value),
            MidpointRounding.AwayFromZero);
        return $"{carChip}  |  all cars {consistTonnes:0} t";
    }

    /// <summary>
    /// Look-at identity: <c>Loco DE2 · 38t · train 184t</c> or freight <c>46t · train 184t</c>.
    /// </summary>
    public static string? FormatInspectIdentity(
        bool isLoco,
        string? locoTypeLabel,
        float? carKilograms,
        float? consistKilograms)
    {
        if (carKilograms is null || carKilograms.Value <= 0f)
        {
            return isLoco ? locoTypeLabel : null;
        }

        var carTonnes = Math.Round(
            KilogramsToTonnes(carKilograms.Value),
            MidpointRounding.AwayFromZero);
        var hasTrain = consistKilograms is float ck
            && ck > carKilograms.Value * 1.01f + 1f;
        var trainTonnes = hasTrain
            ? Math.Round(KilogramsToTonnes(consistKilograms!.Value), MidpointRounding.AwayFromZero)
            : 0;

        if (isLoco)
        {
            var loco = string.IsNullOrWhiteSpace(locoTypeLabel) ? "Loco" : locoTypeLabel!.Trim();
            return hasTrain
                ? $"{loco} · {carTonnes:0}t · train {trainTonnes:0}t"
                : $"{loco} · {carTonnes:0}t";
        }

        return hasTrain
            ? $"{carTonnes:0}t · train {trainTonnes:0}t"
            : $"{carTonnes:0}t";
    }
}
