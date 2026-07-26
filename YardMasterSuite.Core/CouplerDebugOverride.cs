using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Per-car session coupler HUD overrides (keyed by train car id).
/// </summary>
public static class CouplerDebugOverride
{
    private sealed class State
    {
        public CouplerLinkStatus? Front;
        public CouplerLinkStatus? Rear;
    }

    private static readonly Dictionary<string, State> ByCarId = new();

    public static bool HasAnyOverride => ByCarId.Count > 0;

    public static void Cycle(string carId)
    {
        if (string.IsNullOrEmpty(carId))
        {
            return;
        }

        if (!ByCarId.TryGetValue(carId, out var state))
        {
            ByCarId[carId] = new State
            {
                Front = CouplerLinkStatus.MuWarning,
                Rear = CouplerLinkStatus.Linked,
            };
            return;
        }

        if (state.Front == CouplerLinkStatus.MuWarning
            && state.Rear == CouplerLinkStatus.Linked)
        {
            state.Front = CouplerLinkStatus.Linked;
            state.Rear = CouplerLinkStatus.MuWarning;
            return;
        }

        if (state.Front == CouplerLinkStatus.Linked
            && state.Rear == CouplerLinkStatus.MuWarning)
        {
            state.Front = CouplerLinkStatus.MuWarning;
            state.Rear = CouplerLinkStatus.MuWarning;
            return;
        }

        ByCarId.Remove(carId);
    }

    public static void Clear() => ByCarId.Clear();

    public static CouplerLinkStatus? ApplyFront(string? carId, CouplerLinkStatus? real)
    {
        if (!string.IsNullOrEmpty(carId) && ByCarId.TryGetValue(carId!, out var state) && state.Front != null)
        {
            return state.Front;
        }

        return real;
    }

    public static CouplerLinkStatus? ApplyRear(string? carId, CouplerLinkStatus? real)
    {
        if (!string.IsNullOrEmpty(carId) && ByCarId.TryGetValue(carId!, out var state) && state.Rear != null)
        {
            return state.Rear;
        }

        return real;
    }

    public static string StatusFragment(string? carId)
    {
        if (string.IsNullOrEmpty(carId) || !ByCarId.TryGetValue(carId!, out var state))
        {
            return "off";
        }

        return $"F={Label(state.Front)} R={Label(state.Rear)}";
    }

    private static string Label(CouplerLinkStatus? status) =>
        status switch
        {
            CouplerLinkStatus.Linked => "+",
            CouplerLinkStatus.MuTeam => "+B",
            CouplerLinkStatus.Loose => "*",
            CouplerLinkStatus.MuWarning => "*Y",
            CouplerLinkStatus.Open => "-",
            _ => "?",
        };
}
