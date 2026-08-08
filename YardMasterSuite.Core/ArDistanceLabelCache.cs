namespace YardMasterSuite.Core;

/// <summary>
/// AR captions show whole meters, so the string only has to be rebuilt when the rounded distance
/// changes. Rebuilding every frame (60+ fps × every marker) was a top managed-heap source behind the
/// ~2.5 s GC cadence (2026-08-07 video; mod-off A/B confirmed the hitch is ours).
/// </summary>
public static class ArDistanceLabelCache
{
    /// <summary>Whole meters as shown in the caption; negative / unknown clamps to 0.</summary>
    public static int RoundMeters(float? distanceMeters)
    {
        if (distanceMeters is null
            || float.IsNaN(distanceMeters.Value)
            || float.IsInfinity(distanceMeters.Value))
        {
            return 0;
        }

        var meters = distanceMeters.Value;
        return meters <= 0f
            ? 0
            : (int)System.Math.Round(meters, System.MidpointRounding.AwayFromZero);
    }

    public static bool NeedsRebuild(string? cachedLabel, int cachedMeters, int meters) =>
        cachedLabel is null || cachedMeters != meters;

    /// <summary>Radar / job-car captions also carry identity, so those must invalidate too.</summary>
    public static bool NeedsRebuild(
        string? cachedLabel,
        int cachedMeters,
        int meters,
        string? cachedIdentity,
        string? identity) =>
        NeedsRebuild(cachedLabel, cachedMeters, meters)
        || !string.Equals(cachedIdentity, identity, System.StringComparison.Ordinal);
}
