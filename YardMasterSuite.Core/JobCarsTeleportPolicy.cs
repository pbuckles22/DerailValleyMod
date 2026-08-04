namespace YardMasterSuite.Core;

/// <summary>Why job-cars teleport / place was refused (3.1).</summary>
public enum JobCarsTeleportAbort
{
    None = 0,
    NoJob,
    NoCars,
    PartialResolve,
    Moving,
    BusyTeleporting,
    NoTarget,
    Hazmat,
    Unsafe,
}

/// <summary>Pure fail-closed policy for moving existing job cars (3.1).</summary>
public static class JobCarsTeleportPolicy
{
    public const float MaxAbsSpeedKmh = 0.5f;

    /// <summary>
    /// Integrity + safety for a teleport attempt.
    /// <paramref name="resolvedCarCount"/> must equal <paramref name="expectedCarCount"/> (no partial cuts).
    /// </summary>
    public static JobCarsTeleportAbort Evaluate(
        bool hasJob,
        int expectedCarCount,
        int resolvedCarCount,
        float? maxAbsSpeedKmh,
        bool isTeleporting,
        bool hasTargetTrack,
        bool hazmatPresent)
    {
        if (!hasJob)
        {
            return JobCarsTeleportAbort.NoJob;
        }

        if (expectedCarCount <= 0)
        {
            return JobCarsTeleportAbort.NoCars;
        }

        if (resolvedCarCount <= 0 || resolvedCarCount != expectedCarCount)
        {
            return JobCarsTeleportAbort.PartialResolve;
        }

        if (isTeleporting)
        {
            return JobCarsTeleportAbort.BusyTeleporting;
        }

        if (maxAbsSpeedKmh is float speed && speed > MaxAbsSpeedKmh)
        {
            return JobCarsTeleportAbort.Moving;
        }

        if (!hasTargetTrack)
        {
            return JobCarsTeleportAbort.NoTarget;
        }

        if (hazmatPresent)
        {
            return JobCarsTeleportAbort.Hazmat;
        }

        return JobCarsTeleportAbort.None;
    }

    public static bool CanTeleport(JobCarsTeleportAbort abort) => abort == JobCarsTeleportAbort.None;

    public static string FormatAbort(JobCarsTeleportAbort abort) => abort switch
    {
        JobCarsTeleportAbort.None => "OK",
        JobCarsTeleportAbort.NoJob => "no job",
        JobCarsTeleportAbort.NoCars => "no job cars",
        JobCarsTeleportAbort.PartialResolve => "cars unresolved",
        JobCarsTeleportAbort.Moving => "consist moving",
        JobCarsTeleportAbort.BusyTeleporting => "teleport busy",
        JobCarsTeleportAbort.NoTarget => "no track target",
        JobCarsTeleportAbort.Hazmat => "hazmat blocked",
        JobCarsTeleportAbort.Unsafe => "unsafe",
        _ => "blocked",
    };

    public static string FormatPlaceChip(bool placeActive, int carCount, string? trackId, JobCarsTeleportAbort abort)
    {
        if (!placeActive)
        {
            return "";
        }

        if (abort != JobCarsTeleportAbort.None)
        {
            return "PLACE BLOCKED · " + FormatAbort(abort);
        }

        var track = string.IsNullOrWhiteSpace(trackId) ? "—" : trackId!.Trim();
        return $"PLACE OK · {carCount} cars · {track}";
    }
}
