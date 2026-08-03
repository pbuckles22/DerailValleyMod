using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 Posted Limit / Next checks.
/// </summary>
public readonly struct SpeedLimitDebugSnapshot
{
    public SpeedLimitDebugSnapshot(
        bool hasLoco,
        string speed,
        string limit,
        string? detail = null,
        string? changeKey = null)
    {
        HasLoco = hasLoco;
        Speed = speed;
        Limit = limit;
        Detail = detail;
        ChangeKey = changeKey ?? limit;
    }

    public bool HasLoco { get; }
    public string Speed { get; }
    public string Limit { get; }
    public string? Detail { get; }
    public string ChangeKey { get; }

    public string FormatFragment()
    {
        var core = $"{Speed}  |  {Limit}";
        if (!string.IsNullOrEmpty(Detail))
        {
            core = $"{core}  |  {Detail}";
        }

        return core;
    }

    public bool SameAs(SpeedLimitDebugSnapshot other) =>
        HasLoco == other.HasLoco && ChangeKey == other.ChangeKey;
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
