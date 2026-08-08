namespace YardMasterSuite.Core;

/// <summary>
/// AR asks the reader for markers every frame, so cheap-looking "is my cache still valid?" probes
/// ran at frame rate. Walking the player inventory or every station per frame allocated enough to
/// drive the ~2.5 s stop-the-world cadence (2026-08-07 smoke; hitch vanishes with the mod off).
/// Probes answer at a human rate instead; the underlying caches keep their own longer lifetimes.
/// </summary>
public static class PerFrameProbeThrottle
{
    /// <summary>Town proximity moves slowly even at line speed.</summary>
    public const float TownProximitySeconds = 0.5f;

    /// <summary>Picking up or turning in a job may take up to this long to show on AR.</summary>
    public const float JobIdentitySeconds = 0.25f;

    /// <summary>Walking the consist to find "my loco" when the game has no last loco.</summary>
    public const float ConsistResolveSeconds = 0.25f;

    public static bool Due(float secondsSinceLastProbe, float intervalSeconds) =>
        secondsSinceLastProbe >= intervalSeconds;
}
