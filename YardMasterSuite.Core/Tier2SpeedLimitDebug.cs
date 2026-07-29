namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 speed-limit checks.
/// </summary>
public readonly struct SpeedLimitDebugSnapshot
{
    public SpeedLimitDebugSnapshot(bool hasLoco, string speed, string limit, string? detail = null)
    {
        HasLoco = hasLoco;
        Speed = speed;
        Limit = limit;
        Detail = detail;
    }

    public bool HasLoco { get; }
    public string Speed { get; }
    public string Limit { get; }

    /// <summary>Board text / facing dots when a limit change is attributed to a scan hit.</summary>
    public string? Detail { get; }

    public string FormatFragment()
    {
        var core = $"{Speed}  |  {Limit}";
        return string.IsNullOrEmpty(Detail) ? core : $"{core}  |  {Detail}";
    }

    /// <summary>True when loco/limit unchanged — ignore Speed so T2 does not spam every km/h.</summary>
    public bool SameAs(SpeedLimitDebugSnapshot other) =>
        HasLoco == other.HasLoco && Limit == other.Limit;
}

public static class Tier2SpeedLimitDebug
{
    public const string Prefix = "T2 limit";

    public static string? NextLogMessage(SpeedLimitDebugSnapshot? previous, SpeedLimitDebugSnapshot current)
    {
        if (previous is null)
        {
            return $"{Prefix} init ({Where(current)}): {current.FormatFragment()}";
        }

        var prior = previous.Value;
        if (prior.HasLoco != current.HasLoco)
        {
            return $"{Prefix} {Where(current)}: {current.FormatFragment()}";
        }

        if (!prior.SameAs(current))
        {
            return $"{Prefix} change: {current.FormatFragment()}";
        }

        return null;
    }

    private static string Where(SpeedLimitDebugSnapshot snap) =>
        snap.HasLoco ? "loco" : "no-loco";
}
