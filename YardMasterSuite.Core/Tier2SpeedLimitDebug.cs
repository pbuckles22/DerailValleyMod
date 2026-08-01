using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 speed-limit / brake-pacing checks.
/// Drive + Brake-advisory fields are included so hard-brake sessions can be correlated
/// with Limit adopt and advisory level (1.16 lead tuning).
/// </summary>
public readonly struct SpeedLimitDebugSnapshot
{
    public SpeedLimitDebugSnapshot(
        bool hasLoco,
        string speed,
        string limit,
        string? detail = null,
        string? drive = null,
        string? advice = null,
        string? changeKey = null)
    {
        HasLoco = hasLoco;
        Speed = speed;
        Limit = limit;
        Detail = detail;
        Drive = drive;
        Advice = advice;
        ChangeKey = changeKey ?? BuildChangeKey(limit, advice, drive);
    }

    public bool HasLoco { get; }
    public string Speed { get; }
    public string Limit { get; }

    /// <summary>Board text / facing dots when a limit change is attributed to a scan hit.</summary>
    public string? Detail { get; }

    /// <summary>Throttle / brake / pipe / grade snapshot (human-readable).</summary>
    public string? Drive { get; }

    /// <summary>Brake advisory chip summary (level + target + eta).</summary>
    public string? Advice { get; }

    /// <summary>
    /// Equality key for log gating: Limit + advisory level/target + coarse brake bucket.
    /// Speed / fine throttle are omitted so T2 does not spam every km/h.
    /// </summary>
    public string ChangeKey { get; }

    public string FormatFragment()
    {
        var core = $"{Speed}  |  {Limit}";
        if (!string.IsNullOrEmpty(Drive))
        {
            core = $"{core}  |  {Drive}";
        }

        if (!string.IsNullOrEmpty(Advice))
        {
            core = $"{core}  |  {Advice}";
        }

        if (!string.IsNullOrEmpty(Detail))
        {
            core = $"{core}  |  {Detail}";
        }

        return core;
    }

    /// <summary>True when loco/limit/advice/brake-bucket unchanged — ignore Speed wobble.</summary>
    public bool SameAs(SpeedLimitDebugSnapshot other) =>
        HasLoco == other.HasLoco && ChangeKey == other.ChangeKey;

    public static string BuildChangeKey(string limit, string? advice, string? drive)
    {
        var brakeBucket = ExtractBrakeBucket(drive);
        var adviceKey = string.IsNullOrEmpty(advice) ? "adv=—" : advice!;
        // Advice already encodes level+target; keep Limit separate for adopt flashes.
        return $"{limit}|{adviceKey}|br={brakeBucket}";
    }

    /// <summary>Coarse train-brake bucket from a Drive fragment containing <c>Br N%</c>.</summary>
    public static string ExtractBrakeBucket(string? drive)
    {
        if (string.IsNullOrEmpty(drive))
        {
            return "none";
        }

        var idx = drive!.IndexOf("Br ", StringComparison.Ordinal);
        if (idx < 0)
        {
            return "none";
        }

        var start = idx + 3;
        var end = drive.IndexOf('%', start);
        if (end <= start)
        {
            return "none";
        }

        if (!int.TryParse(drive.Substring(start, end - start), out var pct))
        {
            return "none";
        }

        return BrakeBucketFromPercent(pct);
    }

    public static string BrakeBucketFromPercent(int brakePercent)
    {
        if (brakePercent < 5)
        {
            return "idle";
        }

        if (brakePercent < 35)
        {
            return "light";
        }

        if (brakePercent < 65)
        {
            return "medium";
        }

        return "hard";
    }
}

/// <summary>Pure formatters for Limit drive / Brake-advisory T2 fields.</summary>
public static class LimitDriveDebug
{
    public static string FormatDrive(
        float? throttle01,
        float? brake01,
        float? independent01,
        float? pipeBar,
        float? gradePercent,
        float? massTonnes = null,
        string? locomotiveTypeId = null)
    {
        var thr = throttle01 is float t ? $"Thr {(int)Math.Round(t * 100f)}%" : "Thr —";
        var br = brake01 is float b ? $"Br {(int)Math.Round(b * 100f)}%" : "Br —";
        var ind = independent01 is float i ? $"Ind {(int)Math.Round(i * 100f)}%" : "Ind —";
        var pipe = pipeBar is float p ? $"Pipe {p:0.0}" : "Pipe —";
        var grade = gradePercent is float g ? $"grade={g:0.0}%" : "grade=—";
        var mass = massTonnes is float m ? $" mass={m:0.#}t" : string.Empty;
        var type = string.IsNullOrWhiteSpace(locomotiveTypeId)
            ? string.Empty
            : $" type={locomotiveTypeId}";
        return $"{thr} {br} {ind} {pipe} {grade}{mass}{type}";
    }

    public static string FormatAdvice(BrakeAdvisoryState state)
    {
        if (state.Level == BrakeAdvisoryLevel.None)
        {
            return "adv=None";
        }

        var level = state.Level switch
        {
            BrakeAdvisoryLevel.Advisory => "Advisory",
            BrakeAdvisoryLevel.Critical => "Critical",
            BrakeAdvisoryLevel.Runaway => "Runaway",
            _ => state.Level.ToString(),
        };
        return $"adv={level} {state.TargetKmh} in {state.EtaSeconds}s ({state.DistanceMeters}m)";
    }
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
