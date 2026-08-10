namespace YardMasterSuite.Core;

/// <summary>Why job-car AR pins may rebuild (no timer).</summary>
public enum JobCarArScanReason
{
    /// <summary>Same held job — keep prior pins.</summary>
    Keep = 0,

    /// <summary>Picked up or swapped paperwork — resolve pins once.</summary>
    Scan = 1,

    /// <summary>Dropped / no job in hand — clear AR.</summary>
    Clear = 2,
}

/// <summary>
/// Job-car AR is inventory-gated: scan on pickup/swap, clear on drop, never on a clock.
/// </summary>
public static class JobCarArScanPolicy
{
    public static JobCarArScanReason Decide(string? lastScannedJobId, string? currentHeldJobId)
    {
        var held = Normalize(currentHeldJobId);
        var last = Normalize(lastScannedJobId);

        if (held == null)
        {
            return last == null ? JobCarArScanReason.Keep : JobCarArScanReason.Clear;
        }

        if (string.Equals(last, held, System.StringComparison.Ordinal))
        {
            return JobCarArScanReason.Keep;
        }

        return JobCarArScanReason.Scan;
    }

    private static string? Normalize(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return id!.Trim();
    }
}
