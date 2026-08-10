namespace YardMasterSuite.Core;

/// <summary>Switch List step kinds (3.6) — Align target per step.</summary>
public enum SwitchListStepKind
{
    Prep,
    TurnAround,
    Transit,
    Delivery,
}

/// <summary>DTO for Switch List planning — Unity maps DV Job → this (Core stays game-free).</summary>
public sealed class JobSummary
{
    public string JobId { get; set; } = "";
    public string? JobTypeLabel { get; set; }
    public string? OriginYardId { get; set; }
    public string? DestYardId { get; set; }
    public string? OriginTrackId { get; set; }
    public string? DestTrackId { get; set; }
    /// <summary>Optional arrival / through track for Transit; defaults to <see cref="DestTrackId"/>.</summary>
    public string? DestArrivalTrackId { get; set; }
    public bool NeedsTurnAround { get; set; }
    public string? TurntableTrackId { get; set; }
}

/// <summary>One Align leg on a Switch List.</summary>
public sealed class SwitchListStep
{
    public SwitchListStep(int index, SwitchListStepKind kind, string? destYardId, string destTrackId, string label)
    {
        Index = index;
        Kind = kind;
        DestYardId = destYardId;
        DestTrackId = destTrackId;
        Label = label;
    }

    public int Index { get; }
    public SwitchListStepKind Kind { get; }
    public string? DestYardId { get; }
    public string DestTrackId { get; }
    public string Label { get; }
}

/// <summary>Pure job → ordered Switch List steps (3.6).</summary>
public static class SwitchListPlanner
{
    /// <summary>
    /// Fail closed (null) when origin/dest tracks missing, or turn-around requested without a turntable track.
    /// Order: Prep → [TurnAround] → Transit → Delivery.
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<SwitchListStep>? Build(JobSummary? job)
    {
        if (job == null)
        {
            return null;
        }

        var origin = Normalize(job.OriginTrackId);
        var dest = Normalize(job.DestTrackId);
        if (origin == null || dest == null)
        {
            return null;
        }

        string? turntable = null;
        if (job.NeedsTurnAround)
        {
            turntable = Normalize(job.TurntableTrackId);
            if (turntable == null)
            {
                return null;
            }
        }

        var arrival = Normalize(job.DestArrivalTrackId) ?? dest;
        var steps = new System.Collections.Generic.List<SwitchListStep>(4);
        var i = 1;

        steps.Add(new SwitchListStep(i++, SwitchListStepKind.Prep, job.OriginYardId, origin, "Prep → " + origin));

        if (turntable != null)
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.TurnAround,
                job.OriginYardId,
                turntable,
                "Turn around → " + turntable));
        }

        steps.Add(new SwitchListStep(
            i++,
            SwitchListStepKind.Transit,
            job.DestYardId,
            arrival,
            "Transit → " + arrival));

        steps.Add(new SwitchListStep(
            i,
            SwitchListStepKind.Delivery,
            job.DestYardId,
            dest,
            "Delivery → " + dest));

        return steps;
    }

    /// <summary>
    /// Town Turntable Align (manual multi-leg) — optional pivot then turntable.
    /// Fail closed without a turntable track id.
    /// Labels include Set Forward / Set Reverse from facing flags.
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<SwitchListStep>? BuildTownTurntable(
        string? yardId,
        string? turntableTrackId,
        string? pivotTrackId = null,
        bool pivotNeedsReverse = false,
        bool turntableNeedsReverse = false,
        bool insertFacingBeforeTurntable = false)
    {
        var tt = Normalize(turntableTrackId);
        if (tt == null)
        {
            return null;
        }

        var yard = string.IsNullOrWhiteSpace(yardId) ? null : yardId!.Trim();
        var steps = new System.Collections.Generic.List<SwitchListStep>(4);
        var i = 1;
        var pivot = Normalize(pivotTrackId);
        if (pivot != null
            && !string.Equals(pivot, tt, System.StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.Transit,
                yard,
                pivot,
                SwitchListDriveFacing.FormatDriveLabel(pivotNeedsReverse, "Pivot", pivot)));
        }

        if (insertFacingBeforeTurntable
            && pivot != null
            && pivotNeedsReverse != turntableNeedsReverse)
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.Prep,
                yard,
                tt,
                SwitchListDriveFacing.FormatFacingOnlyLabel(turntableNeedsReverse)));
        }

        steps.Add(new SwitchListStep(
            i,
            SwitchListStepKind.TurnAround,
            yard,
            tt,
            SwitchListDriveFacing.FormatDriveLabel(turntableNeedsReverse, "Turn around", tt)));
        return steps;
    }

    private static string? Normalize(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
