using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Per-loco session Load% HUD overrides (keyed by train car id).
/// </summary>
public static class LoadDebugOverride
{
    public const float SmokeWarnPercent = 85f;
    public const float SmokeCriticalPercent = 97f;

    private static readonly Dictionary<string, float> ByCarId = new();

    public static bool HasAnyOverride => ByCarId.Count > 0;

    public static void Cycle(string carId)
    {
        if (string.IsNullOrEmpty(carId))
        {
            return;
        }

        if (!ByCarId.TryGetValue(carId, out var current))
        {
            ByCarId[carId] = SmokeWarnPercent;
            return;
        }

        if (current < SmokeCriticalPercent - 0.5f)
        {
            ByCarId[carId] = SmokeCriticalPercent;
            return;
        }

        ByCarId.Remove(carId);
    }

    public static void Clear() => ByCarId.Clear();

    public static float? Apply(string? carId, float? realPercent)
    {
        if (!string.IsNullOrEmpty(carId) && ByCarId.TryGetValue(carId!, out var forced))
        {
            return forced;
        }

        return realPercent;
    }

    public static string StatusFragment(string? carId)
    {
        if (string.IsNullOrEmpty(carId) || !ByCarId.TryGetValue(carId!, out var forced))
        {
            return "off";
        }

        return $"load={forced:0}%";
    }
}
