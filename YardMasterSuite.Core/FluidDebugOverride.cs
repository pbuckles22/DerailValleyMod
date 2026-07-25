using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Per-loco session HUD fluid overrides (keyed by train car id).
/// </summary>
public static class FluidDebugOverride
{
    public const float SmokeCriticalPercent = 5f;
    public const float SmokeFullPercent = 100f;

    private enum Preset
    {
        Real = 0,
        LowOilFullFuel = 1,
        LowFuelFullOil = 2,
        LowBoth = 3,
        FullBoth = 4,
    }

    private sealed class State
    {
        public Preset Preset;
        public float? Fuel;
        public float? Oil;
    }

    private static readonly Dictionary<string, State> ByCarId = new();

    public static bool HasAnyOverride => ByCarId.Count > 0;

    /// <summary>Cycle fluids for one loco id.</summary>
    public static void Cycle(string carId)
    {
        if (string.IsNullOrEmpty(carId))
        {
            return;
        }

        if (!ByCarId.TryGetValue(carId, out var state))
        {
            state = new State();
            ByCarId[carId] = state;
        }

        state.Preset = state.Preset switch
        {
            Preset.Real => Preset.LowOilFullFuel,
            Preset.LowOilFullFuel => Preset.LowFuelFullOil,
            Preset.LowFuelFullOil => Preset.LowBoth,
            Preset.LowBoth => Preset.FullBoth,
            _ => Preset.Real,
        };
        ApplyPreset(state);

        if (state.Preset == Preset.Real)
        {
            ByCarId.Remove(carId);
        }
    }

    public static void Clear() => ByCarId.Clear();

    public static void Clear(string carId)
    {
        if (!string.IsNullOrEmpty(carId))
        {
            ByCarId.Remove(carId);
        }
    }

    public static float? ApplyFuel(string? carId, float? realPercent) =>
        TryGet(carId, out var s) ? s.Fuel ?? realPercent : realPercent;

    public static float? ApplyOil(string? carId, float? realPercent) =>
        TryGet(carId, out var s) ? s.Oil ?? realPercent : realPercent;

    public static string StatusFragment(string? carId)
    {
        if (!TryGet(carId, out var s) || (s.Fuel is null && s.Oil is null))
        {
            return "off";
        }

        return $"fuel={s.Fuel!.Value:0}% oil={s.Oil!.Value:0}%";
    }

    private static bool TryGet(string? carId, out State state)
    {
        state = null!;
        return !string.IsNullOrEmpty(carId) && ByCarId.TryGetValue(carId!, out state!);
    }

    private static void ApplyPreset(State state)
    {
        switch (state.Preset)
        {
            case Preset.LowOilFullFuel:
                state.Oil = SmokeCriticalPercent;
                state.Fuel = SmokeFullPercent;
                break;
            case Preset.LowFuelFullOil:
                state.Fuel = SmokeCriticalPercent;
                state.Oil = SmokeFullPercent;
                break;
            case Preset.LowBoth:
                state.Fuel = SmokeCriticalPercent;
                state.Oil = SmokeCriticalPercent;
                break;
            case Preset.FullBoth:
                state.Fuel = SmokeFullPercent;
                state.Oil = SmokeFullPercent;
                break;
            default:
                state.Fuel = null;
                state.Oil = null;
                break;
        }
    }
}
