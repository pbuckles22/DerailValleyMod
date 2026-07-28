using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Per-loco session motor-heat HUD/governor overrides for Tier 2 thermal smoke.
/// Forces Hot + MU band without baking TM TEMP on Harbor Hill.
/// </summary>
public static class MotorDebugOverride
{
    /// <summary>Synthetic heat % shown on HUD in Heat50 mode (easy Warning bake).</summary>
    public const float Heat50Percent = 50f;

    public enum Mode
    {
        Off = 0,
        /// <summary>Force Warning Hot — governor uses 75% ceiling.</summary>
        Heat50 = 1,
        /// <summary>Force Critical Hot — governor uses 55% ceiling.</summary>
        Critical = 2,
    }

    private static readonly Dictionary<string, Mode> ByCarId = new();

    public static bool HasAnyOverride => ByCarId.Count > 0;

    public static void Cycle(string carId)
    {
        if (string.IsNullOrEmpty(carId))
        {
            return;
        }

        if (!ByCarId.TryGetValue(carId, out var current))
        {
            ByCarId[carId] = Mode.Heat50;
            return;
        }

        ByCarId[carId] = current switch
        {
            Mode.Heat50 => Mode.Critical,
            _ => Mode.Off,
        };

        if (ByCarId[carId] == Mode.Off)
        {
            ByCarId.Remove(carId);
        }
    }

    public static void Clear() => ByCarId.Clear();

    public static Mode GetMode(string? carId)
    {
        if (string.IsNullOrEmpty(carId) || !ByCarId.TryGetValue(carId!, out var mode))
        {
            return Mode.Off;
        }

        return mode;
    }

    public static MotorStatus? ApplyStatus(string? carId, MotorStatus? real)
    {
        return GetMode(carId) switch
        {
            Mode.Heat50 or Mode.Critical => MotorStatus.Hot,
            _ => real,
        };
    }

    public static MotorCabTempBand? ApplyBand(string? carId, MotorCabTempBand? real)
    {
        return GetMode(carId) switch
        {
            Mode.Heat50 => MotorCabTempBand.Warning,
            Mode.Critical => MotorCabTempBand.Critical,
            _ => real,
        };
    }

    /// <summary>Forced heat % for HUD label, or null when off.</summary>
    public static float? ForcedHeatPercent(string? carId) =>
        GetMode(carId) switch
        {
            Mode.Heat50 => Heat50Percent,
            Mode.Critical => 100f,
            _ => null,
        };

    public static string StatusFragment(string? carId) =>
        GetMode(carId) switch
        {
            Mode.Heat50 => "tm=50% Warning",
            Mode.Critical => "tm=Critical",
            _ => "off",
        };
}
