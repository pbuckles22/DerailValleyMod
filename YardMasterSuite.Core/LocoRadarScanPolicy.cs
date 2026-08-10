namespace YardMasterSuite.Core;

/// <summary>Why a loco-radar FindObjectsOfType scan may run (4.10 hitch diet).</summary>
public enum LocoRadarScanReason
{
    /// <summary>Keep prior cache — no FoT.</summary>
    None = 0,

    /// <summary>Player entered / changed city (yard id).</summary>
    CityEntered = 1,

    /// <summary>Player left a loco (or switched to another). Mark the departed id.</summary>
    LeftLoco = 2,

    /// <summary>UMM toggle / invalidate — one refresh.</summary>
    Forced = 3,
}

/// <summary>
/// Event-gated loco radar scans: one FoT per city, one on leave-loco, never on a timer.
/// Parked locos do not justify rescans; a moving loco the player occupies is already known.
/// </summary>
public static class LocoRadarScanPolicy
{
    /// <summary>
    /// Decide whether to FoT-scan. Updates nothing — caller advances trackers after the call.
    /// When <see cref="LocoRadarScanReason.LeftLoco"/>, <paramref name="leftLocoId"/> is the departed loco.
    /// </summary>
    public static LocoRadarScanReason Decide(
        bool featureEnabled,
        bool forceScan,
        string? lastScannedCityId,
        string? currentCityId,
        int? lastOccupiedLocoId,
        int? currentOccupiedLocoId,
        out int? leftLocoId)
    {
        leftLocoId = null;
        if (!featureEnabled)
        {
            return LocoRadarScanReason.None;
        }

        if (forceScan)
        {
            return LocoRadarScanReason.Forced;
        }

        if (lastOccupiedLocoId.HasValue
            && (!currentOccupiedLocoId.HasValue
                || currentOccupiedLocoId.Value != lastOccupiedLocoId.Value))
        {
            leftLocoId = lastOccupiedLocoId;
            return LocoRadarScanReason.LeftLoco;
        }

        if (!string.IsNullOrWhiteSpace(currentCityId)
            && !CityEquals(lastScannedCityId, currentCityId))
        {
            return LocoRadarScanReason.CityEntered;
        }

        return LocoRadarScanReason.None;
    }

    public static bool CityEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        var left = a!;
        var right = b!;
        return string.Equals(left.Trim(), right.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
