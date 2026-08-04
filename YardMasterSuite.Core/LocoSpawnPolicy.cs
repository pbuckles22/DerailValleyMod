namespace YardMasterSuite.Core;

/// <summary>Why license-gated loco spawn was refused (3.1b).</summary>
public enum LocoSpawnAbort
{
    None = 0,
    NoLiveries,
    NoTarget,
    Busy,
    Unsafe,
}

/// <summary>Pure scroll / chip / abort helpers for loco spawn place mode.</summary>
public static class LocoSpawnPolicy
{
    public static int WrapIndex(int count, int index)
    {
        if (count <= 0)
        {
            return 0;
        }

        var i = index % count;
        return i < 0 ? i + count : i;
    }

    public static int StepIndex(int count, int index, int delta) =>
        WrapIndex(count, index + delta);

    public static LocoSpawnAbort Evaluate(
        int licensedLiveryCount,
        bool hasTargetTrack,
        bool placeBlockedByOverlap,
        bool isBusy)
    {
        if (licensedLiveryCount <= 0)
        {
            return LocoSpawnAbort.NoLiveries;
        }

        if (isBusy)
        {
            return LocoSpawnAbort.Busy;
        }

        if (!hasTargetTrack)
        {
            return LocoSpawnAbort.NoTarget;
        }

        if (placeBlockedByOverlap)
        {
            return LocoSpawnAbort.Unsafe;
        }

        return LocoSpawnAbort.None;
    }

    public static bool CanSpawn(LocoSpawnAbort abort) => abort == LocoSpawnAbort.None;

    public static string FormatAbort(LocoSpawnAbort abort) => abort switch
    {
        LocoSpawnAbort.None => "OK",
        LocoSpawnAbort.NoLiveries => "no licensed locos",
        LocoSpawnAbort.NoTarget => "no track target",
        LocoSpawnAbort.Busy => "spawn busy",
        LocoSpawnAbort.Unsafe => "no space",
        _ => "blocked",
    };

    public static string FormatPlaceChip(
        bool placeActive,
        string? liveryLabel,
        string? trackId,
        LocoSpawnAbort abort)
    {
        if (!placeActive)
        {
            return "";
        }

        if (abort != LocoSpawnAbort.None)
        {
            return "SPAWN BLOCKED · " + FormatAbort(abort);
        }

        var loco = string.IsNullOrWhiteSpace(liveryLabel) ? "—" : liveryLabel!.Trim();
        var track = string.IsNullOrWhiteSpace(trackId) ? "—" : trackId!.Trim();
        return $"SPAWN OK · {loco} · {track}";
    }

    /// <summary>
    /// True for spawn-scroll candidates: loco liveries + handcar.
    /// Excludes Slug / Relic / caboose (free specials that polluted the list).
    /// </summary>
    public static bool IsEligibleSpawnLocoId(string? liveryId)
    {
        var id = liveryId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        if (id!.IndexOf("Slug", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.IndexOf("Relic", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.IndexOf("Caboose", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (id.IndexOf("HandCar", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.Equals("HandCar", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return id.IndexOf("Loco", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Short HUD label from livery id (e.g. LocoDH4 → DH4).</summary>
    public static string ShortLiveryLabel(string? liveryId)
    {
        var id = liveryId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return "—";
        }

        const string locoPrefix = "Loco";
        if (id!.StartsWith(locoPrefix, System.StringComparison.OrdinalIgnoreCase)
            && id.Length > locoPrefix.Length)
        {
            id = id.Substring(locoPrefix.Length);
        }

        return string.IsNullOrEmpty(id) ? "—" : id!;
    }
}
